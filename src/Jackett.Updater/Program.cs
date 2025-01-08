using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;

namespace Jackett.Updater
{
    public class Program
    {
        private IProcessService processService;
        private IServiceConfigService windowsService;
        public static Logger logger;
        private Variants.JackettVariant variant = Variants.JackettVariant.NotFound;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            var runtimeSettings = new RuntimeSettings()
            {
                CustomLogFileName = "updater.txt"
            };

            LogManager.Configuration = LoggingSetup.GetLoggingConfiguration(runtimeSettings);
            logger = LogManager.GetCurrentClassLogger();

            logger.Info("Jackett Updater " + EnvironmentUtil.JackettVersion());
            logger.Info("Options \"" + string.Join("\" \"", args) + "\"");

            var variants = new Variants();
            variant = variants.GetVariant();
            logger.Info("Jackett variant: " + variant.ToString());

            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            if (isWindows)
            {
                //The updater starts before Jackett closes
                logger.Info("Pausing for 3 seconds to give Jackett & tray time to shutdown");
                System.Threading.Thread.Sleep(3000);
            }

            processService = new ProcessService(logger);
            windowsService = new WindowsServiceConfigService(processService, logger);

            var commandLineParser = new Parser(settings => settings.CaseSensitive = false);

            try
            {
                var optionsResult = commandLineParser.ParseArguments<UpdaterConsoleOptions>(args);
                optionsResult.WithParsed(options =>
                {
                    ProcessUpdate(options);
                }
                );
                optionsResult.WithNotParsed(errors =>
                {
                    logger.Error(HelpText.AutoBuild(optionsResult));
                    logger.Error("Failed to process update arguments!");
                    logger.Error(errors.ToString());
                    Console.ReadKey();
                });
            }
            catch (Exception e)
            {
                logger.Error($"Exception applying update!\n{e}");
            }
        }

        private void KillPids(int[] pids)
        {
            foreach (var pid in pids)
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    logger.Info("Killing process " + proc.Id);

                    // try to kill gracefully (on unix) first, see #3692
                    var exited = false;
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                Arguments = "-15 " + pid,
                                FileName = "kill"
                            };
                            Process.Start(startInfo);
                            System.Threading.Thread.Sleep(1000); // just sleep, WaitForExit() doesn't seem to work on mono/linux (returns immediantly), https://bugzilla.xamarin.com/show_bug.cgi?id=51742
                            exited = proc.WaitForExit(2000);
                        }
                        catch (Exception e)
                        {
                            logger.Error($"Error while sending SIGTERM to {pid}\n{e}");
                        }
                        if (!exited)
                            logger.Info($"Process {pid} didn't exit within 2 seconds after a SIGTERM");
                    }
                    if (!exited)
                        proc.Kill(); // send SIGKILL
                    exited = proc.WaitForExit(5000);
                    if (!exited)
                        logger.Info($"Process {pid} didn't exit within 5 seconds after a SIGKILL");
                }
                catch (ArgumentException)
                {
                    logger.Info($"Process {pid} is already dead");
                }
                catch (Exception e)
                {
                    logger.Error($"Error killing process {pid}\n{e}");
                }
            }
        }

        private void ProcessUpdate(UpdaterConsoleOptions options)
        {
            var updateLocation = EnvironmentUtil.JackettInstallationPath();
            if (!(updateLocation.EndsWith("\\") || updateLocation.EndsWith("/")))
                updateLocation += Path.DirectorySeparatorChar;

            var pids = Array.Empty<int>();
            if (options.KillPids != null)
            {
                var pidsStr = options.KillPids.Split(',').Where(pid => !string.IsNullOrWhiteSpace(pid)).ToArray();
                pids = Array.ConvertAll(pidsStr, pid => int.Parse(pid));
            }

            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var trayRunning = false;
            var trayProcesses = Process.GetProcessesByName("JackettTray");
            if (isWindows)
            {
                if (trayProcesses.Length > 0)
                    foreach (var proc in trayProcesses)
                        try
                        {
                            logger.Info($"Killing tray process {proc.Id}");
                            proc.Kill();
                            trayRunning = true;
                        }
                        catch (Exception e)
                        {
                            logger.Error(e);
                        }

                // on unix we don't have to wait (we can overwrite files which are in use)
                // On unix we kill the PIDs after the update so e.g. systemd can automatically restart the process
                KillPids(pids);
            }

            var variants = new Variants();
            if (variants.IsNonWindowsDotNetCoreVariant(variant))
            {
                // On Linux you can't modify an executable while it is executing
                // https://github.com/Jackett/Jackett/issues/5022
                // https://stackoverflow.com/questions/16764946/what-generates-the-text-file-busy-message-in-unix#comment32135232_16764967
                // Delete the ./jackett executable
                // pdb files are also problematic https://github.com/Jackett/Jackett/issues/5167#issuecomment-489301150

                var jackettExecutable = options.Path.TrimEnd('/') + "/jackett";
                var pdbFiles = Directory.EnumerateFiles(options.Path, "*.pdb", SearchOption.AllDirectories).ToList();
                var removeList = pdbFiles;
                removeList.Add(jackettExecutable);

                foreach (var fileForDelete in removeList)
                {
                    try
                    {
                        logger.Info("Attempting to remove: " + fileForDelete);

                        if (File.Exists(fileForDelete))
                        {
                            File.Delete(fileForDelete);
                            logger.Info("Deleted " + fileForDelete);
                        }
                        else
                            logger.Info("File for deleting not found: " + fileForDelete);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                    }
                }
            }

            logger.Info("Finding files in: " + updateLocation);
            var files = Directory.GetFiles(updateLocation, "*.*", SearchOption.AllDirectories).OrderBy(x => x).ToList();
            logger.Info($"{files.Count} update files found");

            try
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file).ToLowerInvariant();

                    if (fileName.EndsWith(".zip") || fileName.EndsWith(".tar") || fileName.EndsWith(".gz"))
                        continue;

                    var fileCopySuccess = CopyUpdateFile(options.Path, file, updateLocation, false);

                    if (!fileCopySuccess) //Perform second attempt, this time removing the target file first
                        CopyUpdateFile(options.Path, file, updateLocation, true);
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            logger.Info("File copying complete");

            // delete old dirs
            var oldDirs = new string[] { "Content/logos" };

            foreach (var oldDir in oldDirs)
            {
                try
                {
                    var deleteDir = Path.Combine(options.Path, oldDir);
                    if (Directory.Exists(deleteDir))
                    {
                        logger.Info("Deleting directory " + deleteDir);
                        Directory.Delete(deleteDir, true);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            // delete old files
            var oldFiles = new string[] {
                "appsettings.Development.json",
                "Autofac.Integration.WebApi.dll",
                "Content/congruent_outline.png",
                "Content/crissXcross.png",
                "Content/css/jquery.dataTables.css",
                "Content/css/jquery.dataTables_themeroller.css",
                "CsQuery.dll",
                "CurlSharp.dll",
                "CurlSharp.pdb",
                "Definitions/01torrent.yml",
                "Definitions/2xfree.yml",
                "Definitions/24rolika.yml",
                "Definitions/32pages.yml",
                "Definitions/3evils.yml",
                "Definitions/3evils-api.yml",
                "Definitions/3wmg.yml",
                "Definitions/420files.yml",
                "Definitions/7torrents.yml",
                "Definitions/academictorrents.yml",
                "Definitions/acervos-api.yml",
                "Definitions/acgsou.yml",
                "Definitions/acidlounge.yml",
                "Definitions/adbt.yml",
                "Definitions/aither.yml", // switch to *-API #8682
                "Definitions/alein.yml",
                "Definitions/alexfilm.yml",
                "Definitions/alleenretail.yml",
                "Definitions/anaschcc.yml",
                "Definitions/angietorrents.yml",
                "Definitions/anidex.yml", // migrated to C#
                "Definitions/anime-free.yml",
                "Definitions/animeclipse.yml",
                "Definitions/animeitalia.yml",
                "Definitions/animetime.yml",
                "Definitions/animetracker.yml",
                "Definitions/animeworld.yml", // switch to *-API #8682
                "Definitions/anitoons.yml",
                "Definitions/aox.yml",
                "Definitions/apollo.yml", // migrated to C# gazelle base tracker
                "Definitions/archetorrent.yml",
                "Definitions/asgaard.yml",
                "Definitions/ast4u.yml", // renamed to animeworld #10578
                "Definitions/asylumshare.yml",
                "Definitions/at12project.yml",
                "Definitions/audiobookbay.yml", // migrated to C#
                "Definitions/audiobooktorrents.yml", // renamed to abtorrents
                "Definitions/avg.yml",
                "Definitions/awesomehd.yml", // migrated to C#
                "Definitions/b2s-share.yml",
                "Definitions/baibako.yml", // renamed rudub #5673
                "Definitions/beitai.yml",
                "Definitions/beyond-hd.yml", // migrated to C# API
                "Definitions/beyond-hd-oneurl.yml", // #12993
                "Definitions/biatch.yml",
                "Definitions/biho.yml",
                "Definitions/bigtorrent.yml", // merged with eStone #12352
                "Definitions/bigtower.yml",
                "Definitions/bit-titan.yml",
                "Definitions/bitbr.yml",
                "Definitions/bitcityreloaded.yml",
                "Definitions/bithq.yml",
                "Definitions/bitme.yml",
                "Definitions/bitnova.yml",
                "Definitions/bitofvalor.yml",
                "Definitions/bitsexy.yml",
                "Definitions/bitstream.yml",
                "Definitions/bittorrentam.yml",
                "Definitions/blubits.yml",
                "Definitions/bluesbrothers.yml",
                "Definitions/blutopia.yml", // switch to *-API #8682
                "Definitions/bolly-api.yml",
                "Definitions/brasiltracker.yml", // migrated to C#
                "Definitions/breizhpeers.yml",
                "Definitions/broadcity.yml",
                "Definitions/brobits.yml",
                "Definitions/brsociety.yml", // switch to *-API #8682
                "Definitions/brsociety-api.yml",
                "Definitions/bt-scene.yml",
                "Definitions/bt4g.yml",
                "Definitions/btbit.yml",
                "Definitions/btdigg.yml",
                "Definitions/btdb.yml",
                "Definitions/bteye.yml",
                "Definitions/btgigs.yml",
                "Definitions/btkitty.yml",
                "Definitions/btmirror.yml",
                "Definitions/btstornet.yml",
                "Definitions/btworld.yml",
                "Definitions/btxpress.yml",
                "Definitions/bulltorrent.yml",
                "Definitions/cartoonchaos.yml",
                "Definitions/casatorrent.yml", // renamed to teamctgame
                "Definitions/casstudiotv.yml",
                "Definitions/ccfbits.yml",
                "Definitions/centraltorrent.yml",
                "Definitions/channelx.yml",
                "Definitions/cheggit.yml",
                "Definitions/chilebt.yml",
                "Definitions/cili180.yml", // renamed to liaorencili
                "Definitions/cilipro.yml",
                "Definitions/cinefilhd.yml",
                "Definitions/cloudtorrents.yml",
                "Definitions/cooltorrent.yml",
                "Definitions/cpasbiensi.yml",
                "Definitions/crazyhd.yml",
                "Definitions/crazyscorner.yml",
                "Definitions/cryptichaven.yml",
                "Definitions/czteam.yml",
                "Definitions/cztorrent.yml",
                "Definitions/danishbytes.yml", // migrated to C#
                "Definitions/dariustracker.yml",
                "Definitions/darktracker.yml",
                "Definitions/darmowetorenty.yml", // migrated to C#
                "Definitions/datascene.yml", // switch to *-API #8682
                "Definitions/datatalli.yml",
                "Definitions/demonoid.yml",
                "Definitions/demonsite.yml",
                "Definitions/denofsimps-api.yml",
                "Definitions/desireleasers.yml",
                "Definitions/desitorrents.yml", // switch to *-API #8682
                "Definitions/devils-playground.yml",
                "Definitions/devilsplayground.yml",
                "Definitions/digbt.yml",
                "Definitions/documentarytorrents.yml",
                "Definitions/dodder.yml",
                "Definitions/downloadville.yml",
                "Definitions/dragonworld.yml",
                "Definitions/dragonworldreloaded.yml",
                "Definitions/dreamteam.yml",
                "Definitions/dxdhd.yml",
                "Definitions/ebookparadijs.yml",
                "Definitions/efectodoppler.yml",
                "Definitions/eggmeon.yml",
                "Definitions/elitehd.yml",
                "Definitions/elitetorrent-biz.yml",
                "Definitions/elitetorrent-pl.yml",
                "Definitions/elittracker.yml",
                "Definitions/emmidtracker.yml",
                "Definitions/emtrek.yml",
                "Definitions/eotforum.yml",
                "Definitions/epizod.yml",
                "Definitions/erzsebet.yml",
                "Definitions/erzsebetpl.yml",
                "Definitions/estrenosdtl.yml",
                "Definitions/ethor.yml",
                "Definitions/ettv.yml",
                "Definitions/evolutionpalace.yml",
                "Definitions/exoticaz.yml", // migrated to C#
                "Definitions/extratorrent-ag.yml",
                "Definitions/extratorrent-cd.yml",
                "Definitions/extratorrent-it.yml",
                "Definitions/extratorrentclone.yml",
                "Definitions/extremetorrents.yml",
                "Definitions/fantasticheaven.yml",
                "Definitions/fastscene.yml",
                "Definitions/feedurneed.yml", // merged with devilsplayground #6872
                "Definitions/fenyarnyek-tracker.yml",
                "Definitions/file-shack.yml",
                "Definitions/filebase.yml",
                "Definitions/filelisting.yml",
                "Definitions/film-paleis.yml",
                "Definitions/filmsclub.yml",
                "Definitions/firebit.yml",
                "Definitions/ftuapps.yml",
                "Definitions/gfxpeers.yml",
                "Definitions/falkonvisionteam.yml",
                "Definitions/focusx.yml",
                "Definitions/fouducinema.yml",
                "Definitions/freakstrackingsystem.yml",
                "Definitions/freedomhd.yml",
                "Definitions/freetorrent.yml",
                "Definitions/freshmeat.yml",
                "Definitions/fullmixmusic.yml",
                "Definitions/funreleases.yml",
                "Definitions/fusionmayhem.yml",
                "Definitions/gainbound.yml",
                "Definitions/galeriens.yml",
                "Definitions/gamera.yml",
                "Definitions/gdf76.yml",
                "Definitions/generationfree.yml", // switch to unit3d api #12982
                "Definitions/gfxnews.yml",
                "Definitions/gods.yml",
                "Definitions/goodtracker.yml",
                "Definitions/gormogon.yml",
                "Definitions/greekdiamond.yml",
                "Definitions/greeklegends.yml",
                "Definitions/greekteam.yml",
                "Definitions/gtorrent.yml",
                "Definitions/h-p2p.yml",
                "Definitions/hachede-c.yml",
                "Definitions/hachede.yml",
                "Definitions/hamsterstudio.yml",
                "Definitions/haresclub.yml",
                "Definitions/hd4free.yml",
                "Definitions/hdai.yml",
                "Definitions/hdbc.yml", // renamed to hdbitscom
                "Definitions/hdbits.yml",
                "Definitions/hdbitscom.yml",
                "Definitions/hdcenter.yml",
                "Definitions/hdchina.yml",
                "Definitions/hdcity.yml", // moved to pixelados
                "Definitions/hdclub.yml",
                "Definitions/hddisk.yml",
                "Definitions/hdfun.yml",
                "Definitions/hdhouse.yml",
                "Definitions/hdmayi.yml",
                "Definitions/hdme.yml",
                "Definitions/hdmonkey.yml",
                "Definitions/hdolimpo.yml", // migrated to UNIT3D API
                "Definitions/hdplus.yml",
                "Definitions/hdreactor.yml", // renamed to hdhouse
                "Definitions/hdspain.yml",
                "Definitions/hdstreet.yml",
                "Definitions/hdvbits.yml",
                "Definitions/hdzone.yml", // renamed to hdfun
                "Definitions/heavens-hell.yml",
                "Definitions/hellastz.yml",
                "Definitions/hidden-palace.yml",
                "Definitions/hon3yhd-net.yml",
                "Definitions/hon3yhd.yml",
                "Definitions/horriblesubs.yml",
                "Definitions/horrorsite.yml",
                "Definitions/hqmusic.yml",
                "Definitions/hqsource.yml",
                "Definitions/hush.yml",
                "Definitions/hyperay.yml",
                "Definitions/ibit.yml",
                "Definitions/icetorrent.yml", // migrated to C# XtremeZone base tracker
                "Definitions/idopeclone.yml",
                "Definitions/ihdbits.yml",
                "Definitions/ilcorsaroverde.yml",
                "Definitions/iloveclassics.yml",
                "Definitions/infinityt.yml",
                "Definitions/inperil.yml",
                "Definitions/isohunt.yml",
                "Definitions/iv-torrents.yml",
                "Definitions/jme-reunit3d.yml", // switch to -API #13043
                "Definitions/jptv.yml", // switch to -API #9242
                "Definitions/kapaki.yml",
                "Definitions/katcrs.yml",
                "Definitions/kaztorka.yml",
                "Definitions/kickasstorrent-kathow.yml", // renamed to kickasstorrents-ws
                "Definitions/kickasstorrent.yml",
                "Definitions/kikibt.yml",
                "Definitions/kimoji.yml",
                "Definitions/kinonavse100.yml",
                "Definitions/kisssub.yml",
                "Definitions/knaben.yml", // migrated to C# API
                "Definitions/lapausetorrents.yml",
                "Definitions/lat-team.yml", // switch to *-API #8682
                "Definitions/latinop2p.yml",
                "Definitions/leaguehd.yml", // renamed to lemonhd
                "Definitions/lechaudron.yml",
                "Definitions/leech24.yml", // switch to *-API #12553
                "Definitions/legacyhd.yml", // renamed to realflix
                "Definitions/legittorrents.yml",
                "Definitions/lemencili.yml",
                "Definitions/leparadisdunet.yml",
                "Definitions/les-cinephiles.yml",
                "Definitions/leporno.yml",
                "Definitions/liaorencili.yml", // renamed to cilipro
                "Definitions/lightning-tor.yml",
                "Definitions/lilleskyorg.yml",
                "Definitions/limetorrentsclone.yml",
                "Definitions/mactorrents.yml",
                "Definitions/magicheaven.yml",
                "Definitions/magico.yml", // renamed to trellas
                "Definitions/magnet4you.yml",
                "Definitions/magnetdl.yml",
                "Definitions/maniatorrent.yml",
                "Definitions/manicomioshare.yml",
                "Definitions/mediamaatjes.yml",
                "Definitions/megabliz.yml",
                "Definitions/mendigosdaweb.yml",
                "Definitions/metal-iplay-ro.yml", // renamed to romanianmetaltorrents
                "Definitions/metalwrks.yml",
                "Definitions/microbit.yml",
                "Definitions/mkvcage.yml",
                "Definitions/moecat.yml",
                "Definitions/mononokebt.yml",
                "Definitions/monova.yml",
                "Definitions/montorrent.yml",
                "Definitions/movcr.yml",
                "Definitions/moviesite.yml",
                "Definitions/movietorrent.yml", // will need c# #11284
                "Definitions/movietorrentz.yml",
                "Definitions/movieworld.yml",
                "Definitions/moviezone.yml", // migrated to teracod #9743
                "Definitions/mteamtp.yml", // migrated to C# (API)
                "Definitions/mteamtp2fa.yml", // migrated to C# (API)
                "Definitions/music-master.yml",
                "Definitions/muziekfabriek.yml",
                "Definitions/nachtwerk.yml",
                "Definitions/nbtorrents.yml",
                "Definitions/nbtorrents-api.yml",
                "Definitions/netcosmo.yml",
                "Definitions/netlab.yml",
                "Definitions/newpct-me.yml",
                "Definitions/nextjav.yml",
                "Definitions/nexttorrent.yml",
                "Definitions/nforce.yml",
                "Definitions/nitro.yml",
                "Definitions/nnm-club.yml", // renamed to noname-club
                "Definitions/nordichd.yml",
                "Definitions/nordicplus.yml",
                "Definitions/nostalgic.yml", // renamed to vhstapes
                "Definitions/nyaa-pantsu.yml",
                "Definitions/nyaa.yml",
                "Definitions/nyoo.yml",
                "Definitions/oasis.yml",
                "Definitions/obscure.yml",
                "Definitions/okfunpt.yml",
                "Definitions/omgwtftrackr.yml",
                "Definitions/oncesearch.yml",
                "Definitions/onlineselfeducation.yml",
                "Definitions/onlyscene.yml",
                "Definitions/oxtorrent.yml",
                "Definitions/p2pelite.yml",
                "Definitions/partis.yml",
                "Definitions/passionetorrent.yml",
                "Definitions/peerjunkies.yml",
                "Definitions/piratadigital.yml",
                "Definitions/piratbit.yml",
                "Definitions/piratbitl.yml",
                "Definitions/pirateiro.yml",
                "Definitions/pixelados.yml",
                "Definitions/pixelados-html.yml",
                "Definitions/pleasuredome.yml",
                "Definitions/plusteca.yml",
                "Definitions/polishsource.yml",
                "Definitions/polishtracker.yml",
                "Definitions/pornbits.yml",
                "Definitions/pornforall.yml",
                "Definitions/pornleech.yml",
                "Definitions/pornolive.yml",
                "Definitions/pornorip.yml",
                "Definitions/pornoslab.yml",
                "Definitions/pornotor.yml",
                "Definitions/portugas.yml", // switch to *-API #13542
                "Definitions/potuk.yml",
                "Definitions/prostylex.yml",
                "Definitions/ps4-torrent.yml",
                "Definitions/pt99.yml",
                "Definitions/ptlsp.yml",
                "Definitions/ptmsg.yml",
                "Definitions/purovicio.yml",
                "Definitions/puurhollands.yml",
                "Definitions/qctorrent.yml",
                "Definitions/qxr.yml",
                "Definitions/racing4everyone.yml", // switch to *-API #12870 #8682
                "Definitions/rapidetracker.yml",
                "Definitions/rarbg.yml", // migrated to C#
                "Definitions/redbits.yml", // switch to *-API #11540 #8682
                "Definitions/redbits-api.yml",
                "Definitions/redtopia.yml",
                "Definitions/reelflix.yml", // switch to *-API #8682
                "Definitions/renegade.yml",
                "Definitions/retroflix.yml", // migrated to C#
                "Definitions/rgu.yml",
                "Definitions/rmhd.yml",
                "Definitions/rns.yml", // site merged with audiobooktorrents
                "Definitions/rockhardlossless.yml",
                "Definitions/rodvd.yml",
                "Definitions/rofd.yml",
                "Definitions/rptorrents.yml",
                "Definitions/rptorrents-api.yml", // renamed to rptscene-api
                "Definitions/rptscene.yml",
                "Definitions/rptscene-api.yml", // changed engine, new indexer rptscene #5368
                "Definitions/rus-media.yml",
                "Definitions/scenefz.yml", // migrated to C# XtremeZone base tracker
                "Definitions/scenehd.yml", // migrated to C# (use JSON API)
                "Definitions/scenelinks.yml",
                "Definitions/scenepalace.yml",
                "Definitions/scenereactor.yml",
                "Definitions/scenexpress.yml",
                "Definitions/sdbits.yml",
                "Definitions/sdkino.yml",
                "Definitions/secretcinema.yml", // migrated to C# gazelle base tracker
                "Definitions/seedpeer.yml",
                "Definitions/sexxi.yml",
                "Definitions/shadowflow.yml",
                "Definitions/sharefiles.yml",
                "Definitions/shareisland.yml", // switch to *-API #8682
                "Definitions/sharespacedb.yml",
                "Definitions/shareuniversity.yml",
                "Definitions/sharewood.yml", // switch to *-API #10269
                "Definitions/sharingue.yml",
                "Definitions/shellife.yml",
                "Definitions/sharkpt.yml",
                "Definitions/shokweb.yml",
                "Definitions/skipthecommercials.yml", // switch to *-API #8682
                "Definitions/skipthecommercials-api.yml",
                "Definitions/skipthetrailers.yml",
                "Definitions/skytorrents-lol.yml",
                "Definitions/skytorrents-to.yml",
                "Definitions/skytorrents.yml",
                "Definitions/skytorrentsclone.yml", // renamed to skytorrents-lol
                "Definitions/skytorrentsclone2.yml", // renamed to skytorrents-to
                "Definitions/slosoul.yml",
                "Definitions/soundpark.yml",
                "Definitions/spacetorrent.yml",
                "Definitions/speed-share.yml",
                "Definitions/speedtorrentreloaded.yml",
                "Definitions/spidertk.yml",
                "Definitions/sporthd.yml",
                "Definitions/sukebei-pantsu.yml",
                "Definitions/swarmazon.yml", //  switched to *-API #13725
                "Definitions/t411.yml",
                "Definitions/t411v2.yml",
                "Definitions/takeabyte.yml",
                "Definitions/tazmaniaden.yml",
                "Definitions/tbplus.yml",
                "Definitions/tehconnection.yml",
                "Definitions/tellytorrent.yml", // switch to *-API #8682
                "Definitions/tellytorrent-api.yml",
                "Definitions/tenyardtracker.yml", // to be migrated to c#, #795
                "Definitions/tfile.yml",
                "Definitions/theaudioscene.yml",
                "Definitions/the-crazy-ones.yml",
                "Definitions/the-devils-lounge.yml",
                "Definitions/thedarkcommunity-api.yml",
                "Definitions/thehorrorcharnel.yml",
                "Definitions/theleachzone.yml", // switch to *-API #11185
                "Definitions/the-madhouse.yml",
                "Definitions/themoviecave.yml",
                "Definitions/therebels-api.yml",
                "Definitions/theresurrection.yml",
                "Definitions/thesceneplace.yml",
                "Definitions/theshinning.yml", // switch to *-API #13751
                "Definitions/theshinning-api.yml",
                "Definitions/thespit.yml",
                "Definitions/thetorrents.yml",
                "Definitions/theunknown.yml", // became 3evils #9678
                "Definitions/thorsland.yml",
                "Definitions/tigers-dl.yml",
                "Definitions/tjangto.yml",
                "Definitions/tntfork.yml",
                "Definitions/tntvillage.yml",
                "Definitions/toonsforme.yml",
                "Definitions/topnow.yml",
                "Definitions/torlook.yml",
                "Definitions/toros.yml", // renamed to bulltorrent
                "Definitions/torrent911.yml", // renamed to oxtorrent-vip #15411
                "Definitions/torrent9clone.yml", // renamed to torrent9-tel #15217
                "Definitions/torrent-paradise-ml.yml",
                "Definitions/torrent4you.yml",
                "Definitions/torrentbomb.yml",
                "Definitions/torrentcouch.yml",
                "Definitions/torrentdb.yml",
                "Definitions/torrentfactory.yml",
                "Definitions/torrentgalaxyorg.yml", // renamed to torrentgalaxy
                "Definitions/torrenthane.yml",
                "Definitions/torrentkim.yml",
                "Definitions/torrentmax.yml",
                "Definitions/torrentparadise.yml",
                "Definitions/torrentproject.yml",
                "Definitions/torrentquest.yml",
                "Definitions/torrentrex.yml",
                "Definitions/torrentsectorcrew.yml",
                "Definitions/torrentseed.yml", // renamed to latinop2p #9065
                "Definitions/torrentseeds.yml", // migrated to c#
                "Definitions/torrentsmd.yml",
                "Definitions/torrent-town.yml",
                "Definitions/torrentsurf.yml", // renamed xtremebytes #9951
                "Definitions/torrentv.yml",
                "Definitions/torrentvault.yml",
                "Definitions/torrentwal.yml",
                "Definitions/torrentwhiz.yml",
                "Definitions/torrentwtf.yml",
                "Definitions/torrentz2.yml",
                "Definitions/torrentz2eu.yml",
                "Definitions/torrentz2k.yml",
                "Definitions/torrof.yml",
                "Definitions/torviet.yml",
                "Definitions/tosky.yml",
                "Definitions/totallykids.yml",
                "Definitions/totheglorycookie.yml", // renamed totheglory2fa
                "Definitions/trackeros-api.yml",
                "Definitions/trackeros.yml", // switch to *-API #12807
                "Definitions/tspate.yml",
                "Definitions/ttobogo.yml",
                "Definitions/ttsweb.yml",
                "Definitions/turknova.yml",
                "Definitions/turkseed.yml",
                "Definitions/turktracker.yml",
                "Definitions/tvroad.yml",
                "Definitions/twilight.yml",
                "Definitions/twilightszoom.yml",
                "Definitions/twisted-music.yml",
                "Definitions/u-torrents.yml",
                "Definitions/uhd-heaven.yml",
                "Definitions/ultimategamerclub.yml",
                "Definitions/ultra-hd.yml",
                "Definitions/ultrahdclub.yml",
                "Definitions/underverse.yml",
                "Definitions/underversel.yml",
                "Definitions/uniotaku.yml", // to be migrated to c#
                "Definitions/universaltorrents.yml",
                "Definitions/unleashthecartoons.yml",
                "Definitions/uploads.yml",
                "Definitions/utorrents.yml", // same as SzeneFZ now
                "Definitions/vanila.yml",
                "Definitions/vhstapes.yml",
                "Definitions/visionhd-api.yml",
                "Definitions/vizuk.yml",
                "Definitions/vtorrent.yml",
                "Definitions/waffles.yml",
                "Definitions/witchhunter.yml",
                "Definitions/worldofp2p.yml",
                "Definitions/worldwidetorrents.yml",
                "Definitions/woot.yml",
                "Definitions/xbit.yml",
                "Definitions/xbytes2.yml",
                "Definitions/xfsub.yml",
                "Definitions/xktorrent.yml",
                "Definitions/xperiencebytes-api.yml",
                "Definitions/xtremefile.yml",
                "Definitions/xtremezone.yml", // migrated to C# XtremeZone base tracker
                "Definitions/xxxtorrents.yml",
                "Definitions/ydypt.yml",
                "Definitions/yingk.yml",
                "Definitions/yourexotic.yml", // renamed to exoticaz
                "Definitions/zooqle.yml",
                "Microsoft.Owin.dll",
                "Microsoft.Owin.FileSystems.dll",
                "Microsoft.Owin.Host.HttpListener.dll",
                "Microsoft.Owin.Hosting.dll",
                "Microsoft.Owin.StaticFiles.dll",
                "Owin.dll",
                "System.ServiceModel.dll",
                "System.Web.Http.dll",
                "System.Web.Http.Owin.dll",
                "System.Web.Http.Tracing.dll",
                "System.Xml.XPath.XmlDocument.dll"
            };

            foreach (var oldFile in oldFiles)
            {
                try
                {
                    var deleteFile = Path.Combine(options.Path, oldFile);
                    if (File.Exists(deleteFile))
                    {
                        logger.Info("Deleting file " + deleteFile);
                        File.Delete(deleteFile);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            // remove .lock file to detect errors in the update process
            var lockFilePath = Path.Combine(options.Path, ".lock");
            if (File.Exists(lockFilePath))
                File.Delete(lockFilePath);

            // kill pids after the update on UNIX
            if (!isWindows)
                KillPids(pids);

            if (!options.NoRestart)
            {
                if (isWindows && (trayRunning || options.StartTray) && !string.Equals(options.Type, "WindowsService", StringComparison.OrdinalIgnoreCase))
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        Arguments = $"--UpdatedVersion \" {EnvironmentUtil.JackettVersion()}\"",
                        FileName = Path.Combine(options.Path, "JackettTray.exe"),
                        UseShellExecute = true
                    };

                    logger.Info("Starting Tray: " + startInfo.FileName + " " + startInfo.Arguments);
                    Process.Start(startInfo);

                    if (!windowsService.ServiceExists())
                    {
                        //User was running the tray icon, but not using the Windows service, starting Tray icon will start JackettConsole as well
                        return;
                    }
                }

                if (string.Equals(options.Type, "WindowsService", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info("Starting Windows service");

                    try
                    {
                        windowsService.Start();
                    }
                    catch
                    {
                        logger.Info("Failed to start service. Attempting to start console.");
                        try
                        {
                            var consolePath = Path.Combine(options.Path, "JackettConsole.exe");
                            processService.StartProcessAndLog(consolePath, "--Start", true);
                        }
                        catch
                        {
                            logger.Error("Failed to start the service or console.");
                        }
                    }
                }
                else
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        Arguments = options.Args,
                        FileName = GetJackettConsolePath(options.Path),
                        UseShellExecute = true
                    };

                    if (isWindows)
                    {
                        //User didn't initiate the update from Windows service and wasn't running Jackett via the tray, must have started from the console
                        startInfo.Arguments = $"/K \"{startInfo.FileName}\" {startInfo.Arguments}";
                        startInfo.FileName = "cmd.exe";
                        startInfo.CreateNoWindow = false;
                        startInfo.WindowStyle = ProcessWindowStyle.Normal;
                    }

                    if (variant == Variants.JackettVariant.Mono)
                    {
                        startInfo.Arguments = $"\"{startInfo.FileName}\" {startInfo.Arguments}";
                        startInfo.FileName = "mono";
                    }

                    if (variant == Variants.JackettVariant.CoreMacOs || variant == Variants.JackettVariant.CoreMacOsArm64
                    || variant == Variants.JackettVariant.CoreLinuxAmdx64 || variant == Variants.JackettVariant.CoreLinuxArm32
                    || variant == Variants.JackettVariant.CoreLinuxArm64
                    || variant == Variants.JackettVariant.CoreLinuxMuslAmdx64 || variant == Variants.JackettVariant.CoreLinuxMuslArm32
                    || variant == Variants.JackettVariant.CoreLinuxMuslArm64)
                    {
                        startInfo.UseShellExecute = false;
                        startInfo.CreateNoWindow = true;
                    }

                    logger.Info($"Starting Jackett: \"{startInfo.FileName}\" {startInfo.Arguments}");
                    Process.Start(startInfo);
                }
            }
        }

        private bool CopyUpdateFile(string jackettDestinationDirectory, string fullSourceFilePath, string updateSourceDirectory, bool previousAttemptFailed)
        {
            var success = false;

            string fileName;
            string fullDestinationFilePath;
            string fileDestinationDirectory;

            try
            {
                fileName = Path.GetFileName(fullSourceFilePath);
                fullDestinationFilePath = Path.Combine(jackettDestinationDirectory, fullSourceFilePath.Substring(updateSourceDirectory.Length));
                fileDestinationDirectory = Path.GetDirectoryName(fullDestinationFilePath);
            }
            catch (Exception e)
            {
                logger.Error(e);
                return false;
            }

            logger.Info($"Attempting to copy {fileName} from source: {fullSourceFilePath} to destination: {fullDestinationFilePath}");

            if (previousAttemptFailed)
            {
                logger.Info("The first attempt copying file: " + fileName + "failed. Retrying and will delete old file first");

                try
                {
                    if (File.Exists(fullDestinationFilePath))
                    {
                        logger.Info(fullDestinationFilePath + " was found");
                        System.Threading.Thread.Sleep(1000);
                        File.Delete(fullDestinationFilePath);
                        logger.Info("Deleted " + fullDestinationFilePath);
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        logger.Info(fullDestinationFilePath + " was NOT found");
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            try
            {
                if (!Directory.Exists(fileDestinationDirectory))
                {
                    logger.Info("Creating directory " + fileDestinationDirectory);
                    Directory.CreateDirectory(fileDestinationDirectory);
                }

                File.Copy(fullSourceFilePath, fullDestinationFilePath, true);
                logger.Info("Copied " + fileName);
                success = true;
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            return success;
        }

        private string GetJackettConsolePath(string directoryPath)
        {
            var variants = new Variants();
            return Path.Combine(directoryPath, variants.IsNonWindowsDotNetCoreVariant(variant) ? "jackett" : "JackettConsole.exe");
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            logger.Error(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}
