import moment from 'moment';
export class MomentValueConverter {
    toView(value, format) {
        return moment(value).format(format);
    }
}
//# sourceMappingURL=MomentValueConverter.js.map