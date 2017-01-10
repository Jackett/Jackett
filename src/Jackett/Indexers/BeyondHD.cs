using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Text;

namespace Jackett.Indexers
{
    public class BeyondHD : BaseIndexer, IIndexer
    {
        private readonly string[] groups = new string[] { "FASM", "FraMeSToR", "SC4R", "HDX", "4KiNGS", "0MNiDVD", "0TV", "10801920", "1920", "2HD", "2PaCaVeLi", "420RipZ", "433", "4F", "4HM", "7SiNS", "850105", "A4O", "ABD", "ABEZ", "ACCLAIM", "ACED", "ADHD", "ADMiRALS", "AEGiS", "AEN", "AERO", "AEROHOLiCS", "AFO", "ALDi", "ALLiANCE", "AMBiTiOUS", "AMIABLE", "AMSTEL", "ANARCHY", "ANGELiC", "ANiHLS", "ANiVCD", "AQUA", "ARCHiViST", "ARROW", "ARTHOUSE", "ARiGOLD", "ARiSCO", "ARiSCRAPAYSiTES", "ASAP", "ASCENDANCE", "ATS", "AUTHORiTY", "AVCDVD", "AVCHD", "AVS720", "AZuRRaY AaS", "AiRTV", "AiRWAVES", "ApL", "Argon", "AsiSter", "AzNiNVASiAN", "B4F", "BAJSKORV", "BALLS", "BAND1D0S", "BARGE", "BATV", "BAVARiA", "BAWLS", "BBDvDR", "BDP", "BELiAL", "BETAMAX", "BFF", "BLOW", "BLUEYES", "BLooDWeiSeR", "BOV", "BOW", "BRASTEMP", "BRAVERY", "BREiN", "BRICKSQUaD", "BRMP", "BRUTUS", "BRiGAND", "BRiGHT", "BULLDOZER", "BURGER", "BWB", "BaCKToRG", "BaCo", "BaLD", "BareHD", "BeStDivX", "BestHD", "BiA", "BiEN", "BiERBUiKEN", "BiFOS", "BiGBruv", "BiGFiL", "BiL", "BiPOLAR", "BiQ", "BiRDHOUSE", "BountyHunters", "BrG", "C4TV", "CABs", "CAMELOT", "CAMERA", "CBFD", "CBFM", "CBGB", "CCAT", "CCCAM", "CCCUNT", "CDDHD", "CFE", "CFH", "CG", "CHAMPiONS", "CHRONO", "CHaWiN", "CLASSiC", "CLUE", "CLiX", "CME", "CNHD", "COALiTiON", "COCAIN", "COMPULSiON", "CONDITION", "CONSCiENCE", "CREST", "CRONiC", "CROOKS", "CROSSBOW", "CRiMSON", "CTU", "CULTXviD", "CURIOSITY", "CYBERMEN", "CaRaT", "CarVeR", "Catchphraser", "Centropy", "Chakra", "CiA", "CiELO", "CiNEFiLE", "CiNEVCD", "CiPA", "CiRCLE", "CiTRiN", "Cinemaniacs", "CoWRY", "Counterfeit", "D0NK", "D0PE", "D2V", "D3Si", "DAA", "DAH", "DASH", "DCP", "DDX", "DEADPOOL", "DEADPiXEL", "DEAL", "DEFACED", "DEFEATER", "DEFiNiTE", "DEMAND", "DEMENTED", "DEPRAViTY", "DEPRiVED", "DERANGED", "DETAiLS", "DEUTERiUM", "DEiMOS", "DEiTY", "DFA", "DGX", "DHANi", "DHD", "DIE", "DIMENSION", "DMT", "DNA", "DNR DOCERE", "DOCUMENT", "DOMiNATE", "DOMiNO", "DOMiNiON", "DOWN", "DPiMP", "DRABBITS", "DRAWER", "DREAMLiGHT", "DRHD", "DROiDS", "DTFS", "DUKES", "DUPLI", "DVL", "DcN", "DeBCz", "DeBTViD", "DeBijenkorf", "DiAMOND", "DiCH", "DiFFERENT", "DiSPLAY", "DiVERGE", "DiVERSE", "DiVXCZ", "DiViSiON", "DigitalVX", "DioXidE", "DnB", "DoNE", "DoR2", "DvF", "DvP EDRP", "EDUCATiON", "ELiA", "EMERALD", "ENCOUNTERS", "EPiC", "EPiSODE", "ERyX", "ESPN", "ESPiSE", "ETACH", "ETHOS", "EUSTASS", "EVOLVE", "EViLISO", "EXCELLENCE", "EXECUTION", "EXIST3NC3", "EXT", "EXViD", "EXiLE", "EiTheL", "EwDp", "FA", "FADE", "FAIRPLAY", "FARGIRENIS", "FCC", "FEVER", "FFM", "FFNDVD", "FHD", "FIXIT", "FKKHD", "FKKTV", "FL", "FLATLiNE", "FLAiR", "FLAiTE", "FLHD", "FQM", "FRAGMENT", "FRIGGHD", "FRiSPARK", "FSiHD", "FTC", "FTP", "FUA", "FULLSiZE", "FUtV", "FZERO", "FaNSuB", "FaiLED", "Felony", "FiCO", "FiHTV", "FilmHD", "FliK", "FmE", "FoA", "FoReVer", "FoV", "GAYGAY", "GAYTEAM", "GDR", "GECKOS", "GENESIDE", "GENUiNE", "GERUDO", "GFW", "GHOULS", "GL", "GM4F", "GOOGLE", "GORE", "GOTEi", "GOTHiC", "GTVG", "GUFFAW", "GUYLiAN", "GZCrew", "GZP", "GeiWoYIZhangPiAOBA", "GreenBlade", "H2", "HACO", "HAFVCD", "HAGGiS", "HALCYON", "HANGOVER", "HANNIBAL", "HAiDEAF", "HCA", "HD1080", "HD4U", "HDCLASSiCS", "HDCP", "HDEX", "HDMI", "HDR", "HDX", "HDi", "HILSWALTB", "HLS", "HOLiDAY", "HTO", "HUBRIS", "HUNTED", "HV", "HYBRiS", "HiGHTiMES", "HooKah", "Hype", "IMMERSE", "INNOCENCE", "INQUISITION", "INSECTS", "ITG", "JACKVID", "JAG", "JAR", "JETSET", "JFKXVID", "JHD", "JKR", "JMT", "JUMANJi", "Jackal", "Japhson", "JoLLyRoGeR", "KAMERA", "KART3LDVD", "KEG", "KFV", "KILLERS", "KJS", "KNOCKOUT", "KNiFESHARP", "KOENiG", "KON", "KSi", "KYR", "KaKa", "KmF", "KuDoS", "LAJ", "LAP", "LCHD", "LD", "LEVERAGE", "LEViTY", "LMAO", "LMG", "LOGiES", "LOL", "LOST", "LPD", "LPH", "LRC", "LUSO", "LYCAN", "Larceny", "LiBRARiANS", "LiGHTNiNG", "LiLDiCK", "LiNE", "LiViDiTY", "Ltu", "M14CH0", "MACHD", "MACK4", "MAGiCAL", "MAGiCViBE", "MANGACiTY", "MARS", "MATCH", "MEDDY", "MEDiAMANiACS", "MEDiEVAL", "MELON", "MELiTE", "METiS", "MHQ", "MOAB", "MOBTV", "MOMENTUM", "MOTION", "MSE", "MULTiPLY", "MVM", "MVN", "MVS", "MaM", "MainEvent", "MaxHD", "MeTH", "MiND", "MiNDTHEGAP", "MiNT", "MiSFiTS", "MoA", "MoF", "MoH", "MoTv", "NANO", "NBCTV", "NBS", "NCAXA", "NDRT", "NEPTUNE", "NERDHD", "NGB", "NGCHD", "NGR", "NHH", "NJKV", "NLSGDVDr", "NODLABS", "NOHD", "NORDiCHD", "NORiTE", "NOSCREENS", "NSN", "NVA", "NaWaK", "NeDiVx", "NewMov", "NoLiMiT", "OEM", "OEM1080", "OMER", "OMERTA", "OMiCRON", "OPTiC", "OPiUM", "ORC", "ORCDVD", "ORENJi", "ORGANiC", "ORiGiNAL", "OSiRiS", "OSiTV", "P0W4", "P0W4DVD", "P2W", "PANZeR", "PARASiTE", "PARTiCLE", "PCH", "PELLUCiD", "PFa", "PHASE", "PHOBOS", "PLANET3", "PREMiER", "PROGRESS", "PROMiSE", "PROVOKE", "PROXY", "PRiNCE", "PSV", "PSYCHD", "PTi", "PUCKS", "PUKKA", "PURE", "PUZZLE", "PVR", "PaTHe", "PiRATE", "PiX", "PoT", "PostX", "QCF", "QRUS", "QSP", "QiM", "QiX", "R3QU3ST", "RAP", "RCDiVX", "RDVAS", "REACTOR", "REFiNED", "REGEXP", "RELEASE", "REMARKABLE", "REMAX", "REPRiS", "RETRO", "REVEiLLE", "REVOLTE", "REWARD", "REiGN", "RF", "RFtA", "ROVERS", "RRH", "RSG", "RTA", "RTL", "RUBY", "RUNNER", "RUSTLE RedBlade", "Replica", "Republic", "RiFF", "RiTALiN", "RiTALiX", "RiVER", "S0LD13R", "SADPANDA", "SAPHiRE", "SATIVA", "SAiMORNY", "SAiNTS", "SBC", "SCARED", "SCREAM", "SChiZO", "SD6", "SECTOR7", "SEMTEX", "SEPTiC", "SERUM", "SEVENTWENTY", "SFM", "SHDXXX", "SHOCKWAVE", "SHORTBREHD", "SHUNPO", "SKA", "SKANK", "SKGTV", "SLeTDiVX", "SML", "SNOW", "SODAPOP", "SONiDO", "SPARKS", "SPAROOD", "SPLiNTER", "SPLiTSViLLE", "SPOCHT", "SPRiNTER", "SQUEAK", "SQUEEZE", "SRP", "SRiZ", "SSF", "STRONG", "SUBMERGE", "SUBTiTLES", "SUM", "SUN", "SUNSPOT", "SUPERiER", "SVD", "SVENNE", "SViNTO", "SWAGGERHD", "SWOLLED", "SYNS", "SYS", "SiBV", "SiNNERS", "SiTiN", "SomeTV", "StyleZz", "SuPReME", "SweWR", "TARS", "TASTE", "TASTETV", "TAXES", "TBS", "TBZ", "TCM", "TCPA", "TDF", "TEKATE", "TELEFUNKEN", "TELEViSiON", "TENEIGHTY", "TERRA", "TFE", "TFiN", "TGP", "THENiGHTMAREiNHD", "THUGLiNE", "TINKERS", "TLA", "TNAN", "TNS", "TOPAZ", "TOPCAT", "TRG", "TRUEDEF", "TRexHD", "TRiPS", "TUBE", "TURBO", "TURKiSO", "TUSAHD", "TVA", "TVBOX", "TVBYEN", "TVLoO", "TVP", "TVSLiCES", "TViLLAGE", "TWCiSO", "TWG", "TWiST", "TWiZTED", "TXF", "Taurine", "ThEdEaDLiVe", "TheBatman", "TheFrail", "TheWretched", "TiDE", "TiMELORDS", "TiMTY", "TiTANS", "Tiggzz", "TiiX", "ToF", "TrV", "TrickorTreat", "TvNORGE", "TxxZ", "UBiK", "ULF", "UMF", "UNSKiLLED", "UNTOUCHABLES", "UNTOUCHED", "UNVEiL", "UNiQUE", "URTV", "USi", "UTOPiA", "VALiOMEDiA", "VAMPS", "VCDVaULT", "VH-PROD", "VIDEOSLAVE", "VST", "Vcore", "VeDeTT", "ViD", "ViLD", "ViRA", "ViTE", "VideoCD", "VoMiT", "VxTXviD", "W4F", "W4Y", "WAF", "WALTERWHITE", "WASTE", "WAT", "WAVEY", "WEST", "WHEELS", "WHiSKEY", "WLM", "WNN", "WPi", "WRD", "WaLMaRT", "WastedTime", "WeFaiLED", "WhoKnow", "WiCKED", "WiDE", "WiKi", "Wizeguys", "XMF", "XOR", "XORBiTANT", "XPERT_HD", "XPRESS", "XR5", "XSTREEM", "XTV", "XanaX", "XviK", "YCDV", "YesTV", "ZMG", "ZZGtv", "aAF", "aGGr0", "aNBc", "aTerFalleT", "aWake", "c0nFuSed", "cNLDVDR", "euHD", "iBEX", "iFH", "iFN", "iFPD", "iGNiTE", "iGNiTiON", "iHATE", "iHD", "iKA", "iLG", "iLLUSiON", "iLM", "iLS", "iMBT", "iMCARE", "iMMORTALS", "iMOVANE", "iMSORNY", "iNCLUSION", "iNCiTE", "iND", "iNFAMOUS", "iNFiNiTE", "iNGOT", "iNSPiRE", "iNSPiRED", "iNTENTiON", "iNTiMiD", "iNVANDRAREN", "iNjECTiON", "iOM", "iSG", "iVL", "intothevoid", "m00tv", "nDn", "sPHD", "ss", "sweHD", "thebeeb", "tlf", "uAuViD", "uDF", "vRs", "waznewz", "x0DuS", "xCZ", "xD2V", "xSCR", "xV", "2Maverick", "2T", "3LTON", "449", "A4N", "ABH", "AFFY", "AFG", "AJP69", "ALANiS", "ALeSiO", "AHD", "AREA11", "AURA", "Abjex", "Absinth", "AltHD", "Anime-Koi", "Asenshi", "ATHD", "AyoSuzy", "BB", "BDClub", "BDCop", "BF1", "BLiN", "BORDERLiNE", "BRrip", "BS", "BTT", "BYTE", "BgFr", "BitHQ", "Blu-bits", "BluDragon", "BluHD", "BluWave", "BlueBird", "BluntzRip", "BlurayDesuYo", "Bob", "C-W", "CBM", "CHD", "CHDBits", "CHiNJiTSU", "CLARiTY", "CLDD", "CMS", "CMSSide", "CNN", "COR", "CP", "CREATiVE24", "CYRUS", "Cache", "CasStudio", "ChaosBlades", "Chihiro", "Chotab", "Chyuu", "Coalgirls", "Commie", "CrEwSaDe", "Cthuko", "CtrlHD", "D-Z0N3", "DEViSE", "DLBR", "DOLEMiTE", "DON", "DWJ", "DX-TV", "DaViEW", "DameDesuYo", "DiFUN", "DigitalDelboy", "DmonHiro", "DoA", "Doki", "Doremi", "ECI", "EDL", "ELANOR", "ESiR", "EV1LAS", "EbP", "Ebi", "EiMi", "Eileen", "EuReKA", "Euc", "EucHD", "EveTaku", "Exiled-Destiny", "F", "FANT", "FASM", "FFF", "FLAWL3SS", "FREAKS", "FTW-HD", "FZHD", "FiNCH", "Final8", "FoRM", "FraMeSToR", "GAGE", "GTi", "Gazdi", "GoLDSToNE", "Gogeta", "Grassy", "Green", "HANDJOB", "HD2DVD", "HDAccess", "HDB", "HDBT", "HDCLUB", "HDLiTE", "HDME", "HDMaNiAcS", "HDR", "HDRoad", "HDS", "HDSpain", "HDWTV", "HDWinG", "HDmonSK", "HDxT", "HERO", "HPotter", "HQC", "HRD", "HT", "HTTV", "HWD", "HWE", "Hatsuyuki", "HeBits", "Hector", "HiDt", "HiFi", "Hiryuu", "HoodBag", "HorribleSubs", "Hukumuzuku", "INtL", "IWStreams", "Introspective", "JCH", "JIVE", "JiZZA", "JohnGalt", "Juggalotus", "JungleBoy", "KAGA", "KCRT", "KLAXXON", "KRaLiMaRKo", "Kaylith", "KiNGS", "KiSHD", "KingBen", "LEGiON", "LOAD", "LTRG", "LiBERTY", "Link420", "LucianaGil", "MAoS", "M0ar", "MD", "MEECH", "MMI", "MOS", "MW", "MYSELF", "MegaJoey", "Mezashite", "MiCDROP", "MiMa", "Migoto", "MissDream", "NFHD", "NT", "NY2", "NhaNc3", "NorTV", "NovaRip", "NuMbErTw0", "Nub", "NyanTaku", "OOO", "OZC", "Oosh", "PISTA", "PLAYNOW", "PLRVRTX", "POD", "PPKORE", "PRoDJi", "PSiG", "PWE", "Pcar", "Penumbra", "PerfectionHD", "Phr0stY", "Pikanet128", "Piranha", "Poke", "PublicHD", "QUEENS", "RAS", "RCG", "RDF", "RED", "RKSTR", "RUDOS", "RVLTN2012", "RZF", "Raizel", "ReDone", "Reborn4HD", "RedJohn", "Rizlim", "Ryu", "SANTi", "SC", "SDH", "SFH", "SHiELD", "SLiME", "SMODOMiTE", "SOAP", "SPASM", "SWC", "SbR", "Secludedly", "Shadowman", "SiC", "Silver007", "Sir.Paul", "Softfeng", "StarryNights", "Sticky83", "Sweet-Star", "TAR", "TB", "THORA", "THoRCuATo", "TM", "TOPKEK", "TRASH", "TRiAL", "TSTN", "TsH", "TT", "TTL", "TVC", "TVChaosUK", "TVV", "TYT", "TayTO", "TeamCoCo", "TmG", "TheBox", "TiGHTBH", "TjHD", "TorrenTGui", "TrollHD", "TrollUHD", "TrueHD", "TxN", "UNPOPULAR", "UTW", "Underwater", "Vawn", "ViLLAiNS", "ViPER", "ViSiON", "VietHD", "VioletKEK", "Vivid", "WB", "WBS", "WDTeam", "WHR", "WHiiZz", "WINNEBAGO", "WLR", "WRCR", "WTB", "WYW", "Weby", "WhyNot", "XAA", "XEON", "XWT", "YFN", "YellowBeast", "YoHo", "Yonidan", "ZR1", "Zurako", "aB", "aXXo", "adzman", "bLinKkY", "beAst", "booomer", "cLT", "coldhell", "de[42]", "decibeL", "denpa", "eMperor", "gc04", "george.c", "gg", "h264iRMU", "iLL", "iMPUDiCiTY", "iNVULTUATiON", "iPOP", "iSuX", "iTRY", "jAh", "jhonny2", "k3n", "kingofosiris", "lulz", "mHD", "mSD", "matt42", "nHD", "nSD", "panos", "pcsyndicate", "reaperdk123", "sHoTV", "saMMie", "tNe", "tnikita", "tonic", "wAm", "xander", "yAzMMA", "NTb", "MX", "SA89", "H@M", "10801920", "2HD", "4F", "7SiNS", "850105", "aAF", "aBD", "ADHD", "AERO", "ALLiANCE", "AMBiTiOUS", "AMIABLE", "ANGELiC", "ASAP", "AVCHD", "AVS720", "BAJSKORV", "BALLS", "BARGE", "BAWLS", "BestHD", "BiA", "BLOW", "BLUEYES", "BRICKSQUaD", "BRiGHT", "BRMP", "BWB", "c0nFuSed", "C4TV", "CBGB", "CDDHD", "CiNEFiLE", "CLASSiC", "COMPULSiON", "CRiMSON", "CROSSBOW", "D3Si", "DAH", "DEFiNiTE", "DEPRAViTY", "DHD", "DiFFERENT", "DIMENSION", "DiVERGE", "DRHD", "DUPLI", "EiTheL", "ENCOUNTERS", "ETHOS", "euHD", "EUSTASS", "FAIRPLAY", "FaNSuB", "FCC", "Felony", "FHD", "FilmHD", "FKKTV", "FoV", "FQM", "FRIGGHD", "FSiHD", "FTP", "GTVG", "H2", "haggis", "HAiDEAF", "HALCYON", "HCA", "HD1080", "HD4U", "HDEX", "HDX", "HiGHTiMES", "HILSWALTB", "HUBRIS", "HV", "HYBRiS", "iBEX", "iGNiTiON", "IMMERSE", "iMSORNY", "iND", "iNFAMOUS", "iNGOT", "iNVANDRAREN", "Japhson", "JMT", "KaKa", "KYR", "LCHD", "LEVERAGE", "LMAO", "LOL", "LOST", "MACHD", "MACK4", "MAGiCAL", "MAGiCViBE", "MELiTE", "MeTH", "METiS", "MHQ", "MiNDTHEGAP", "MiSFiTS", "MOAB", "MOMENTUM", "MSE", "NERDHD", "NGCHD", "NODLABS", "NOHD", "NVA", "OEM", "OEM1080", "OMiCRON", "ORENJI", "P0W4", "PELLUCiD", "PFa", "PREMiER", "PSV", "PURE", "PUZZLE", "QCF", "QSP", "RAP", "REFiNED", "REMAX", "REWARD", "RiVER", "RTA", "SAiMORNY", "SECTOR7", "SEMTEX", "SEVENTWENTY", "SFM", "SHDXXX", "SHORTBREHD", "SIBV", "SiNNERS", "SiTiN", "sPHD", "SSF", "SUN", "SUNSPOT", "SWAGGERHD", "SYS", "TASTETV", "TENEIGHTY", "TERRA", "THENiGHTMAREiNHD", "THUGLiNE", "TiMELORDS", "TiMTY", "TiTANS", "TLA", "TUSAHD", "TVP", "TWiZTED", "URTV", "VAMPS", "ViD", "ViLD", "W4F", "WASTE", "WAVEY", "WEST", "WHEELS", "WiKi", "WLM", "WPi", "XPERT_HD", "XSTREEM", "YesTV", "ZMG", "ZZGtv", "3LTON", "AFG", "AHD", "ALeSiO", "aXXo", "BDClub", "Blu-bits", "BlueBird", "BluWave", "BRrip", "BTT", "CHD", "CHDBits", "CrEwSaDe", "CtrlHD", "D-Z0N3", "de[42]", "decibeL", "DEViSE", "DON", "EbP", "EiMi", "ESiR", "EuReKA", "FASM", "FLAWL3SS(retired)", "FoRM", "FraMeSToR", "FTW-HD", "FZHD", "GAGE", "Gazdi", "george.c", "Gogeta", "GoLDSToNE", "HD2DVD", "HDBT", "HDClub", "HDLiTE", "HDMaNiAcS", "HDME", "HDmonSK", "HDR", "HDxT", "HiDt", "HiFi", "HoodBag", "iLL", "INtL", "KingBen", "KiNGS", "KLAXXON", "LTRG", "mHD", "mSD", "NhaNc3", "nHD", "nSD", "PerfectionHD", "PRODJi", "RUDOS", "SANTi", "Shadowman", "SiC", "Softfeng", "TorrenTGui", "TRASH", "TrollHD", "ViPER", "ViSiON", "WHiiZz", "xander", "YellowBeast", "YoHo", "HDVid", "HQMi", "BLiNK", "DrSi", "HuN-No1", "Hun-TvDay", "HuN-TRiNiTY", "BMF", "decibeL", "D-Z0N3", "FTW-HD", "HiFi", "NCmt", "OlSTiLe", "Penumbra", "Positive", "SHeNTo", "Taxfour", "Titanic", "Rosum", "Zezoo", "HunterX", "Barcelona", "Zorro", "Thering", "HDMOVIE", "Rosum", "monsifdx", "Khaled94", "OSN", "ABOURINED", "BRANCO", "djomati", "HaGraS", "IICaeSarII", "KimoHD", "klashinkov", "MR.KEY.HD", "SALAHHD", "scoot", "ShimalHD", "YosefJoo", "BoOoOoDa", "Gad", "JamesBond", "MiDo007", "Excellence", "yasser540", "YasTon", "clouds", "cdhaty", "EMPEROR", "JaGUaR", "HighQuality", "ModyAdmin", "Miro", "WamaEgypT", "djomati", "Monster", "BAFADEM", "JamesBond", "SaSa", "comanda", "GHELBO", "marotheking", "Prodji RG", "FASM", "FraMeSToR", "SC4R", "HDX", "4KiNGS", "BitHD", "HDxT", "PriMeHD", "Grond", "WiNNy", "BluPanther", "BReWeRS", "ASPHiXiAS", "TeamSuW", "BluHD", "3DNORD", "PrimeHD", "RealHD", "BluEvo", "Jack", "Ruxi", "Jem", "NTb", "HiSD", "BHO", "No1", "TRiNiTY", "CROwn", "BluRG", "IcTv", "DUS", "TBH", "CzT", "d4EUTeAm", "GMT4U Team", "Silver Bullet Upload Team (S.B.U.T)", "DDR", "DrC", "DUS", "ExDR", "IcTv", "M2Tv", "TmG", "TeamTolly", "TDBB", "xDM", "UNiTY", "UNiTYSERiER. AQOS", "eStone", "playHD", "playTV", "playMUSIC", "playON", "playMB", "playSD", "FooKaS", "ACAB", "GLTeam", "NetCrawlers", "GrLTv", "GLM", "MyToG", "GTRD", "PLRVRTX", "HDBriSe", "BobOki", "RightSiZE", "SpaceHD", "Boss", "BluPanther", "CRiSPY", "HDSpace", "HDCLUB", "HiDt", "ViSTA", "HDMaNiAcS", "KRaLiMaRKo", "BluDragon", "UberHD", "HDMike", "gc04", "KESH", "Fl4me", "HDVN", "AllSportsHD", "YoHo", "HDArea", "EPiC", "HDApad", "HDATV. HDCN. HDBiger", "HDBigerTV", "(C)Z", "AE", "AJ8", "AJP", "Arucard", "AtZLIT", "AW", "Azul", "BBW", "BG", "BoK", "Cache", "Chotab", "CJ", "CRiSC", "Cristi", "Crow", "CtrlHD", "CyCR0", "D4", "DChighdef", "DeblocKING", "DiGG", "DiR", "DiRTY", "disc", "DBO", "DON", "DoNOLi", "EA", "EbP", "Eby", "ESiR", "ETH", "EucHD", "ExY", "FANDANGO", "fLAMEhd", "FSK", "Ft4U", "fty", "Funner", "GMoRK", "GoLDSToNE", "Green", "greenHD", "H2", "h264iRMU", "HALYNA", "HDB", "HDC", "HDBiRD", "HDL", "HDxT", "H@M", "hymen", "HZ", "iCO", "iLL", "IMDTHS", "iNFLiKTED", "iNK", "iOZO", "J4F", "JAVLiU", "JCH", "jTV", "k2", "KolHD", "Krispy", "KTN", "KweeK", "Lesnick", "LiNG", "LolHD", "lulz", "M794", "madoff", "MAGiC", "martic", "McFly", "MCR", "MdM", "MDR", "MeDDlER", "MMI", "Moshy", "Mojo", "NaRB", "NiP", "NiX", "nmd", "NorTV", "NTb", "NWO", "OAS", "ONYX", "pB", "PerfectionHD", "PHiN", "PiNG", "PiMP", "PiPicK", "Positive", "Prestige", "Prime", "PXE", "QDP", "quaz", "QXE", "RDK123", "Redµx", "REPTiLE", "RightSiZE", "RuDE", "RZF", "S26", "SbR", "SG", "sJR", "SK", "Skazhutin", "SLO", "SMoKeR", "somedouches", "SbY", "SrS", "SSG", "SuBHD", "TayTO", "tBit", "ThD", "THORA", "tK", "TM", "toho", "Tree", "tRuAVC", "tRuEHD", "TSE", "TsH", "UioP", "UxO", "V", "VanRay", "VietHD", "ViNYL", "WESTSiDE", "WiHD", "XSHD", "yadong1985", "YanY", "Z", "Zim'D", "HDC", "NERDS", "Tvr", "HDWing", "HDWTV", "iHD", "HDChina", "kishd", "NoVA", "NoPA", "TLF", "HDme", "INTL", "iCandy", "FourGHD", "Ruxi", "MeRCuRY", "DGN", "HDL", "PHD", "HDQueen", "MySilu", "HDROAD", "HDS", "HDSTAR", "HDSPAD", "4HDO", "MLN", "PHOENiX", "DBHD", "KiKi", "ONLY", "PEWE", "GrupoHDS", "HDPter", "PbK", "LEGi0N", "MarGe", "GF44", "beAst", "GHiA", "Archmage", "HDU", "DeamoN", "JoN", "Soltu", "Hon3y", "HQM", "MRHQ", "OwL", "HDTime", "ELiTE", "HDS", "HDSPad", "HDSTV", "Reborn4HD", "PSYPHER", "PHDR", "HDRush", "TheVortex", "JsR", "BluPanther", "MZ0N3", "Geek", "Tron Hyper", "PureTV", "TronTV", "iMusic", "Neon", "Original", "Classic HyPad", "VisualArt", "IMT", "Xtreme", "Ft4U", "LTTi", "Vendetta", "Kuryu", "QalesYa", "QOS", "SubZero. JoyHD", "LTRG", "LCC", "Gepont", "KiSHD", "BMDru", "HDStar", "HDTime", "Pack", "MTeam MTeamPAD", "OneHD", "Dracula", "GBL", "MOLY", "ViP3R", "m2g", "OpenCD", "LLM", "KHQ", "PxHD", "PxEHD", "Px3D", "PxHD-Mobies", "Epsilon", "UTR-HD", "HDBEE", "SKALiWAGZ", "TBB", "HANDJOB", "HRiP", "CrEwSaDe", "RZ-RG", "RESEVIL", "DEFLATE", "CtrlSD", "RR", "MMI", "HYPE CalibeR", "hPLuS", "FZHD", "FZMusic", "CiNEMANiA", "GENESIS", "CONFiDENT", "XPRESS", "SiMPLE", "diversity", "MutzNutz", "scott24", "WiKi", "NGB", "DoA", "BDClub", "OoKU", "VietHD", "KiD", "EPiK", "EbP", "TPTB", "BaKeR", "YRP", "NoGrp", "TERACOD", "KuKaS", "Yoshi", "HuN-vetike", "Hun-Gege", "DEFUSED", "Mayday", "POE", "LoC", "EmlHDTeam", "Castellano", "TRCKHD", "HaB", "TayTO", "PIS", "UHDRemux", "TaiTO", "PULSE", "TMB", "GAIA", "WiHD", "FURAX", "LFN", "HGR", "XTSF", "DiRTY", "ViKAT", "Bunny", "Chotab", "VaAr3", "Soul", "Nero9", "Green", "JENC", "tRuEHD", "IJR", "JUSTFORFUN", "CARPEDiEM", "XtremeHD" };
        private readonly string[] stripReplace = new string[] { "Blu-ray", "HD-DVD", "WEB-DL", "VC-1", "h.264", "h.265", "DTS5.1", "DTS.5.1", "DTS 5.1", "DD5.1", "DD.5.1", "DD 5.1", "DTS-X", "HD-MA", "HD.MA" };
        private readonly string[] stripReplaceBoundary = new string[] { "720i", "720p", "1080i", "1080p", "2160p", "4320p", "2K", "8K", "4K", "UHD", "BluRay", "REMUX", "WEBRip", "HDDVD", "HDTV", "WEB", "WEBDL", "BRip", "BRRip", "BDRip", "HDRip", "VC1", "AVC", "HEVC", "x264", "x265", "h264", "h265", "BD25", "BD50", "DTS", "DD", "FLAC", "TrueHD", "Atmos", "HDMA", "MPEG2", "MPEG4", "DXVA", "3D" };

        private string SearchUrl { get { return SiteLink + "browse.php?searchin=title&incldead=0&"; } }

        new ConfigurationDataLoginLink configData
        {
            get { return (ConfigurationDataLoginLink)base.configData; }
            set { base.configData = value; }
        }

        public BeyondHD(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "BeyondHD",
                description: "Without BeyondHD, your HDTV is just a TV",
                link: "https://beyond-hd.me/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataLoginLink())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";

            configData.DisplayText.Value = "Go to the general tab of your BeyondHD user profile and create/copy the Login Link.";

            AddCategoryMapping(37, TorznabCatType.MoviesBluRay); // Movie / Blu-ray
            AddMultiCategoryMapping(TorznabCatType.Movies3D,
                71,  // Movie / 3D
                83 // FraMeSToR 3D
            );
            AddMultiCategoryMapping(TorznabCatType.MoviesHD,
                77, // Movie / 1080p/i
                94, // Movie / 4K
                78, // Movie / 720p
                54, // Movie / MP4
                17, // Movie / Remux
                50, // Internal / FraMeSToR 1080p
                75, // Internal / FraMeSToR 720p
                49, // Internal / FraMeSToR REMUX
                61, // Internal / HDX REMUX
                86, // Internal / SC4R
                95, // Nightripper 1080p
                96, // Nightripper 720p
                98 // Nightripper MicroHD
            );

            AddMultiCategoryMapping(TorznabCatType.TVHD,
                40, // TV Show / Blu-ray
                44, // TV Show / Encodes
                48, // TV Show / HDTV
                89, // TV Show / Packs
                46, // TV Show / Remux
                45, // TV Show / WEB-DL
                97 //  Nightripper TV Show Encodes
            );

            AddCategoryMapping(36, TorznabCatType.AudioLossless); // Music / Lossless
            AddCategoryMapping(69, TorznabCatType.AudioMP3); // Music / MP3
            AddMultiCategoryMapping(TorznabCatType.AudioVideo,
                55, // Music / 1080p/i
                56, // Music / 720p
                42 // Music / Blu-ray
            );


        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            
            var result = await RequestStringWithCookies(configData.LoginLink.Value);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("Welcome Back"), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        // Tracker search is very broken, searching for anything but a movie name often results in missing results
        // We remove various keyowords and filter for them later to work around this issue
        private string CleanupQueryString(string queryString)
        {
            // remove release groups from the end of the string
            foreach (var group in groups)
            {
                if (queryString.EndsWith(group))
                {
                    queryString = queryString.Substring(0, queryString.Length - group.Length);
                    break;
                }
            }

            // remove simple replace keywords (containing special characters)
            foreach (var keyword in stripReplace)
            {
                queryString = queryString.Replace(keyword, "");
            }

            // remove boundary words
            Regex SplitRegex = new Regex("[^a-zA-Z0-9]+");
            var queryStringParts = SplitRegex.Split(queryString);

            var queryStringPartsFiltered = new List<string>();
            foreach (var queryStringPart in queryStringParts)
            {
                if (!stripReplaceBoundary.Contains(queryStringPart))
                {
                    queryStringPartsFiltered.Add(queryStringPart);
                }
            }

            queryString = string.Join("%", queryStringPartsFiltered);

            return queryString;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", CleanupQueryString(searchString));
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
            }

            searchUrl += queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            await FollowIfRedirect(results);
            try
            {
                CQ dom = results.Content;
                var rows = dom["table.torrenttable > tbody > tr.browse_color"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qRow = row.Cq();

                    var catStr = row.ChildElements.ElementAt(0).FirstElementChild.GetAttribute("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    var qLink = row.ChildElements.ElementAt(2).FirstChild.Cq();
                    release.Link = new Uri(SiteLink + "/" + qLink.Attr("href"));
                    var torrentId = qLink.Attr("href").Split('=').Last();

                    var descCol = row.ChildElements.ElementAt(3);
                    var qCommentLink = descCol.FirstChild.Cq();
                    release.Title = qCommentLink.Text();

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Comments = new Uri(SiteLink + "/" + qCommentLink.Attr("href"));
                    release.Guid = release.Comments;
                    release.Link = new Uri($"{SiteLink}download.php?torrent={torrentId}");

                    var dateStr = descCol.ChildElements.Last().Cq().Text().Split('|').Last().ToLowerInvariant().Replace("ago.", "").Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.ChildElements.ElementAt(7).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(10).Cq().Text()) + release.Seeders;

                    var files = qRow.Find("td:nth-child(6)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(9) > a").Get(0).FirstChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    release.DownloadVolumeFactor = 0; // ratioless
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);

                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
