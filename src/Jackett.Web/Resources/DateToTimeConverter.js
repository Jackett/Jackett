import 'moment';
export class DateToTimeConverter {
    toView(value, format) {
        return moment(value).format(format);
    }
}
//# sourceMappingURL=DateToTimeConverter.js.map