import 'moment'

export class DateToTimeConverter {
    toView(value: Date, format: string) {
        return moment(value).format(format);
    }
}