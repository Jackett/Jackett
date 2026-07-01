import { formatDate, formatFileSize } from './utils.ts';

/**
 * @param {Handlebars} Handlebars
 */
export default function(Handlebars) {
    Handlebars.registerHelper('dateFormat', (context, block) => {
        return formatDate(context, block.hash.format || "YYYY-MM-DD HH:mm:ss");
    });

    Handlebars.registerHelper('jackettTimespan', (timestamp, _) => {
        const from = new Date(timestamp);
        const now = new Date();
        const diffMs = now - from;

        if (diffMs < 0) {
            return 'just now';
        }

        const minutes = diffMs / (1000 * 60);
        if (minutes < 120) {
            return `${Math.round(minutes)}m ago`;
        }

        const hours = diffMs / (1000 * 60 * 60);
        if (hours < 48) {
            return `${parseFloat(hours.toFixed(1))}h ago`;
        }

        const days = diffMs / (1000 * 60 * 60 * 24);
        if (days < 365) {
            return `${Math.round(days)}d ago`;
        }

        const years = days / 365;
        return `${Math.round(years)}y ago`;
    });

    Handlebars.registerHelper('jackettSize', (context, _) => formatFileSize(context));

    Handlebars.registerHelper('jackettLogColor', (context, _) => {
        if (context === 'Error')
            return 'danger';
        else if (context === 'Warn')
            return 'warning';
    });

    Handlebars.registerHelper('if_eq', function(a, b, opts) {
        return a == b ? opts.fn(this) : opts.inverse(this);
    });

    Handlebars.registerHelper('if_in', function(elem, list, opts) {
        return list?.includes(elem) ? opts.fn(this) : opts.inverse(this);
    });
}
