import { Toast, Tooltip } from "bootstrap";
import { ApiColumnMethods } from "datatables.net-bs5";
import { api } from "./api.js";
import $ from "jquery";

export interface IndexerCap {
    ID: string;
    Name: string;
}

export interface Indexer {
    id: string;
    name: string;
    description: string;
    type: 'public' | 'private' | 'semi-private';
    configured: boolean;
    site_link: string;
    alternativesitelinks: string[];
    language: string;
    tags: string[];
    last_error: string;
    potatoenabled: boolean;
    caps: IndexerCap[];
    rss_host?: string;
    torznab_host?: string;
    potato_host?: string;
    state?: string;
    type_label?: string;
    mains_cats?: string;
}

interface Filter {
    id: string;
    apply: (indexer: Indexer) => boolean;
    value: string;
}

export const config = {
    basePath: '',
    baseUrl: ''
};

export const indexers = {
    all: [] as Indexer[],
    configured: [] as Indexer[],
    unconfigured: [] as Indexer[],

    setAllIndexers(indexers: Indexer[]) {
        this.all = indexers;
        this.configured = indexers.filter(item => item.configured);
        this.unconfigured = indexers.filter(item => !item.configured);
    },

    updateIndexerState(id: string, state: string) {
        const updateInArray = (array: Indexer[]) => {
            const indexer = array.find(x => x.id === id);
            if (indexer) {
                indexer.state = state;
            }
        };

        updateInArray(this.all);
        updateInArray(this.configured);
        updateInArray(this.unconfigured);
    },

    clear() {
        this.all = [];
        this.configured = [];
        this.unconfigured = [];
    }
};

export const tags = {
    configured: [] as string[],

    setConfiguredTags(indexers: Indexer[]) {
        this.configured = indexers.map(i => i.tags).reduce((a, g) => a.concat(g), []).filter((v, i, a) => a.indexOf(v) === i).sort();
    },

    clear() {
        this.configured = [];
    }
};

export const filters = {
    available: [] as Filter[],
    current: null as string | null,

    setAvailableFilters(indexers: Indexer[]) {
        this.available = [];

        const add = (f: Filter) => {
            if (this.available.find(x => x.id == f.id))
                return;
            if (!indexers.every(f.apply, f) && indexers.some(f.apply, f))
                this.available.push(f);
        };

        this.available.push({ id: "test:passed", apply: state_filter, value: "success" });
        this.available.push({ id: "test:failed", apply: state_filter, value: "error" });

        ["public", "private", "semi-private"]
            .map(t => { return { id: `type:${t}`, apply: type_filter, value: t }; })
            .forEach(add);

        tags.configured.sort()
            .map(t => {
                return {
                    id: `tag:${t.toLowerCase()}`,
                    apply: tag_filter,
                    value: t
                };
            })
            .forEach(add);
    },

    setCurrentFilter(filter: string | null) {
        this.current = filter;
    },

    clear() {
        this.available = [];
        this.current = null;
    }
};

export function formatFileSize(bytes: number, round = 2) {
    if (bytes === 0)
        return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / k ** i).toFixed(round))} ${sizes[i]}`;
}

export function formatDate(date: string, format: string) {
    const d = new Date(date);
    if (isNaN(d.getTime()))
        return date;

    const formatMap: Record<string, string> = {
        'YYYY': d.getFullYear().toString(),
        'MM': String(d.getMonth() + 1).padStart(2, '0'),
        'DD': String(d.getDate()).padStart(2, '0'),
        'HH': String(d.getHours()).padStart(2, '0'),
        'mm': String(d.getMinutes()).padStart(2, '0'),
        'ss': String(d.getSeconds()).padStart(2, '0')
    };

    return format.replace(/YYYY|MM|DD|HH|mm|ss/g,
        match => formatMap[match] ?? match);
}

export function getHashArgs(): Record<string, string | undefined> {
    const hash = location.hash.slice(1);
    if (!hash)
        return {};

    return Object.fromEntries(
        hash.split('&').map(item => {
            const [key, value] = item.split('=', 2);
            return [
                decodeURIComponent(key),
                value !== undefined
                    ? decodeURIComponent(value.replace(/\+/g, ' '))
                    : undefined
            ];
        })
    );
}

export function resolveUrl(baseUrl: string, url: string) {
    if (!baseUrl)
        return url;
    try { return new URL(url, baseUrl).href; }
    catch { return baseUrl + url; }
}

export function copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
        notify("Copied to clipboard!", "success", "fa fa-check");
    }).catch(err => {
        notify(`Failed to copy to clipboard: ${err}`, "danger", "fa fa-exclamation-triangle");
    });
}

export function notify(
    message: string,
    type: 'success' | 'warning' | 'danger' | 'info',
    icon: string,
    autoHide: boolean = true) {
    const toastId = `toast-${++toastCount}`;
    const iconClass = icon || '';

    let typeClass = '';
    switch (type) {
        case 'success': typeClass = 'bg-success text-white'; break;
        case 'danger': typeClass = 'bg-danger text-white'; break;
        case 'warning': typeClass = 'bg-warning text-dark'; break;
        case 'info': typeClass = 'bg-info text-white'; break;
        default: typeClass = 'bg-primary text-white';
    }

    const toastContainer = document.querySelector('.toast-container')!;
    const toastElement = document.createElement('div');
    toastElement.setAttribute('id', toastId);
    toastElement.className = `toast ${typeClass}`;
    toastElement.setAttribute('role', 'alert');
    toastElement.setAttribute('aria-live', 'assertive');
    toastElement.setAttribute('aria-atomic', 'true');

    const inner = document.createElement('div');
    inner.className = 'd-flex';

    const body = document.createElement('div');
    body.className = 'toast-body';

    const iconSpan = document.createElement('span');
    iconSpan.className = `${iconClass} me-2`;
    body.appendChild(iconSpan);
    body.appendChild(document.createTextNode(` ${message}`));

    const closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.className = 'btn-close me-2 m-auto';
    closeBtn.setAttribute('data-bs-dismiss', 'toast');
    closeBtn.setAttribute('aria-label', 'Close');

    inner.appendChild(body);
    inner.appendChild(closeBtn);
    toastElement.appendChild(inner);
    toastContainer.appendChild(toastElement);

    const toast = new Toast(toastElement, { autohide: autoHide });
    toast.show();
    toastElement.addEventListener('hidden.bs.toast', toastElement.remove);
}

export function clearNotify() {
    document.querySelectorAll('.toast').forEach(element => {
        const toast = Toast.getInstance(element);
        if (toast) toast.hide(); else element.remove();
    });
}

let toastCount = 0;

export function createDropDownHtml(column: ApiColumnMethods<any>, exactMatch: boolean) {
    const select = document.createElement('select');
    const option = document.createElement('option');
    option.value = '';
    option.textContent = 'Show all';
    select.appendChild(option);

    const footer = column.footer();
    footer.replaceChildren();
    footer.appendChild(select);

    select.addEventListener('change', () => {
        const val = escape(select.value);

        if (exactMatch) {
            column
                .search(val ? `^${val}$` : '', true, false)
                .draw();
        } else {
            column
                .search(val ? val : '', true, false)
                .draw();
        }
    });

    select.addEventListener('click', (e) => {
        e.stopPropagation();
    });

    // Return jQuery object to maintain compatibility with existing code
    return $(select);
}

const savedPresetKey = "jackett_saved_presets";

export function getSavedPresets(): string[] {
    if (JSON && localStorage) {
        const lsSavedPresets = localStorage.getItem(savedPresetKey);
        return lsSavedPresets !== null ? JSON.parse(lsSavedPresets) : [];
    } else {
        return [];
    }
}

export function setSavedPresets(presets: string[]) {
    if (JSON && localStorage) {
        localStorage.setItem(savedPresetKey, JSON.stringify(presets));
    }
}

export function setSavePresetsButtonState(table, element: HTMLElement, state = false) {
    const button = element.querySelector("button[id='jackett-search-results-datatable_savepreset_button']");
    if (!button) return;

    // Remove previous event listeners by cloning the element
    const newButton = button.cloneNode(true) as HTMLElement;
    button.parentNode?.replaceChild(newButton, button);

    newButton.className = state ? "btn btn-danger btn-sm" : "btn btn-success btn-sm";

    const handleClick = () => {
        const inputSearch = element.querySelector("input[type='search']") as HTMLInputElement;
        if (!inputSearch) return;

        const preset = inputSearch.value.trim();
        if (!preset) return;

        const presets = getSavedPresets();

        if (state) {
            if (presets.includes(preset)) {
                const updatedPresets = presets.filter(item => item !== preset);
                setSavedPresets(updatedPresets);
                const datalist = element.querySelector("datalist[id='jackett-search-saved-presets']");
                datalist?.replaceChildren();
            }
        } else if (!presets.includes(preset)) {
            const updatedPresets = [...presets, preset];
            setSavedPresets(updatedPresets);
        }
        table.api().draw();
    };

    newButton.addEventListener("click", handleClick);
}

export function stableFocus(element: HTMLElement) {
    if (!element) {
        return;
    }
    const rect = element.getBoundingClientRect();
    const inView = (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
    if (inView)
        element.focus();
}

function type_filter(this: Filter, indexer: Indexer) {
    return indexer.type == this.value;
}

function tag_filter(this: Filter, indexer: Indexer) {
    return indexer.tags.map(t => t.toLowerCase()).includes(this.value.toLowerCase());
}

function state_filter(this: Filter, indexer: Indexer) {
    return indexer.state == this.value;
}

export function formatIndexerData(item: Indexer): Indexer {
    item.rss_host = resolveUrl(config.baseUrl, `${config.basePath}/api/v2.0/indexers/${item.id}/results/torznab/api?apikey=${api.key}&t=search&cat=&q=`);
    item.torznab_host = resolveUrl(config.baseUrl, `${config.basePath}/api/v2.0/indexers/${item.id}/results/torznab/`);
    item.potato_host = resolveUrl(config.baseUrl, `${config.basePath}/api/v2.0/indexers/${item.id}/results/potato/`);

    item.state = item.last_error ? "error" : "success";

    if (item.type == "public") {
        item.type_label = "success";
    } else if (item.type == "private") {
        item.type_label = "danger";
    } else if (item.type == "semi-private") {
        item.type_label = "warning";
    } else {
        item.type_label = "default";
    }

    const main_cats_list = item.caps.filter((c: IndexerCap) => parseInt(c.ID) < 100_000).map((c: IndexerCap) => c.Name.split("/")[0]);
    item.mains_cats = [...new Set(main_cats_list)].join(", ");

    return item;
}

export function notifyError(indexerId: string, errorMessage: string, errorEvent: string) {
    if (!errorMessage) {
        notify(`An error occurred while ${errorEvent} indexers, please take a look at indexers with failed test for more information.`,
            "danger", "fa fa-exclamation-triangle");
        return;
    }
    let githubRepo = "Jackett/Jackett";
    let githubText = "this indexer";
    const githubTemplate = "?template=bug_report.yml&";
    if (errorMessage.includes("FlareSolverr")) {
        githubRepo = "FlareSolverr/FlareSolverr";
        githubText = "FlareSolverr";
    }
    const githubUrl = `https://github.com/${githubRepo}/issues/new${githubTemplate}title=[${indexerId}] (${errorEvent})`;
    const indexEnd = 2000 - githubUrl.length; // keep url <= 2k #5104
    const htmlEscapedError = (() => {
        const div = document.createElement('div');
        div.textContent = errorMessage.substring(0, indexEnd);
        return div.innerHTML;
    })();
    const urlEscapedError = encodeURIComponent(errorMessage.substring(0, indexEnd));
    const link = errorMessage.includes("FlareSolverr is not configured")
        ? `<i><a href="https://github.com/Jackett/Jackett#configuring-flaresolverr" target="_blank" rel="noopener noreferrer">Instructions to install and configure FlareSolverr.</a><i>
               <br />
               <i><a href="https://github.com/Jackett/Jackett/wiki/Troubleshooting#error-connecting-to-flaresolverr-server" target="_blank" rel="noopener noreferrer">Troubleshooting frequent errors with FlareSolverr.</a><i>`
        : `<i><a href="${githubUrl} ${urlEscapedError}" target="_blank" rel="noopener noreferrer">Click here to open an issue on GitHub for ${githubText}.</a><i>`;
    notify(`An error occurred while ${errorEvent} this indexer<br /><b>${htmlEscapedError}</b><br />${link}`,
        "danger", "fa fa-exclamation-triangle", false);
}

export function updateReleasesRow(row: Element) {
    const labels = $(row).find("span.release-labels");
    const TitleLink = $(row).find("td.Title > a");
    const IMDBId = $(row).data("imdb");
    const TMDBId = $(row).data("tmdb");
    const TVDBId = $(row).data("tvdb");
    const TVMazeId = $(row).data("tvmaze");
    const TraktId = $(row).data("trakt");
    const DoubanId = $(row).data("douban");
    const Poster = $(row).data("poster");
    const Description = $(row).data("description");
    const DownloadVolumeFactor = parseFloat($(row).find("td.DownloadVolumeFactor").html());
    const UploadVolumeFactor = parseFloat($(row).find("td.UploadVolumeFactor").html());
    const Cat = $(row).find("td.Cat").html();

    let TitleTooltip = "";
    if (Poster)
        TitleTooltip += `<img src='${Poster}' /><br />`;
    if (Description)
        TitleTooltip += Description;

    if (TitleTooltip) {
        new Tooltip(TitleLink[0], {
            title: TitleTooltip,
            html: true,
            placement: "auto"
        });
    }

    labels.empty();

    if (IMDBId) {
        const imdbLen = (IMDBId.toString().length > 7) ? 8 : 7;
        labels.append(`\n<a href="https://www.imdb.com/title/tt${(`0000000${IMDBId}`).slice(-imdbLen)}/" target="_blank" rel="noopener noreferrer" class="label label-imdb" alt="IMDB" title="IMDB">IMDB</a>`);
    }

    if (TMDBId && TMDBId > 0) {
        const TMdbType = (Cat.includes("Movies")) ? "movie" : "tv";
        labels.append(`\n<a href="https://www.themoviedb.org/${TMdbType}/${TMDBId}" target="_blank" rel="noopener noreferrer" class="label label-tmdb" alt="TMDB" title="TMDB">TMDB</a>`);
    }

    if (TVDBId && TVDBId > 0) {
        labels.append(`\n<a href="https://thetvdb.com/?tab=series&id=${TVDBId}" target="_blank" rel="noopener noreferrer" class="label label-tvdb" alt="TVDB" title="TVDB">TVDB</a>`);
    }

    if (TVMazeId && TVMazeId > 0) {
        labels.append(`\n<a href="https://tvmaze.com/shows/${TVMazeId}" target="_blank" rel="noopener noreferrer" class="label label-tvmaze" alt="TVMaze" title="TVMaze">TVMaze</a>`);
    }

    if (TraktId && TraktId > 0) {
        const TraktType = (Cat.includes("Movies")) ? "movies" : "shows";
        labels.append(`\n<a href="https://www.trakt.tv/${TraktType}/${TraktId}" target="_blank" rel="noopener noreferrer" class="label label-trakt" alt="Trakt" title="Trakt">Trakt</a>`);
    }

    if (DoubanId && DoubanId > 0) {
        labels.append(`\n<a href="https://movie.douban.com/subject/${DoubanId}" target="_blank" rel="noopener noreferrer" class="label label-douban" alt="Douban" title="Douban">Douban</a>`);
    }

    if (!isNaN(DownloadVolumeFactor)) {
        if (DownloadVolumeFactor == 0) {
            labels.append('\n<span class="label label-success">FREELEECH</span>');
        } else if (DownloadVolumeFactor < 1) {
            labels.append(`\n<span class="label label-primary">${(DownloadVolumeFactor * 100).toFixed(0)}%DL</span>`);
        } else if (DownloadVolumeFactor > 1) {
            labels.append(`\n<span class="label label-danger">${(DownloadVolumeFactor * 100).toFixed(0)}%DL</span>`);
        }
    }

    if (!isNaN(UploadVolumeFactor)) {
        if (UploadVolumeFactor == 0) {
            labels.append('\n<span class="label label-warning">NO UPLOAD</span>');
        } else if (UploadVolumeFactor != 1) {
            labels.append(`\n<span class="label label-info">${(UploadVolumeFactor * 100).toFixed(0)}%UL</span>`);
        }
    }
}

export function updateTestState(
    id: string,
    state: 'success' | 'error' | 'inprogress',
    message: string | null,
    parent: HTMLElement
) {
    const btn = parent.querySelector(`.indexer-button-test[data-id="${id}"]`);
    if (!btn) return;

    let sortmsg = message;
    if (!sortmsg || state == "success")
        sortmsg = "";

    const td = btn.closest("td");
    if (!td) return;
    td.setAttribute("data-sort", sortmsg);
    td.setAttribute("data-filter", sortmsg);

    if (message) {
        const tooltipInstance = Tooltip.getInstance(btn);
        if (tooltipInstance) {
            tooltipInstance.hide();
            tooltipInstance.dispose();
        }
        btn.setAttribute("title", message);

        new Tooltip(btn, {
            title: message
        });
    }

    const icon = btn.querySelector("span");
    if (icon) {
        icon.classList.remove("fa-check", "fa-exclamation-triangle", "text-primary", "text-success", "text-danger", "spinner-border", "spinner-border-sm");

        if (state == "success") {
            icon.classList.add("fa", "fa-check", "text-success");
        } else if (state == "error") {
            icon.classList.add("fa", "fa-exclamation-triangle", "text-danger");
        } else if (state == "inprogress") {
            icon.classList.add("spinner-border", "spinner-border-sm", "text-primary");
        }
    }

    const dt = $.fn.dataTable.tables({
        visible: true,
        api: true
    }).rows().invalidate('dom');
    if (state != "inprogress")
        dt.draw();

    indexers.updateIndexerState(id, state);
}

export function escape(text: string): string {
    return $.fn.dataTable.util.escapeRegex(text);
}
