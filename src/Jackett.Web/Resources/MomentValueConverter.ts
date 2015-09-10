import moment from 'moment';

export class MomentValueConverter  {
    toView(value: Date, format: string) {
        return moment(value).format(format);
    }
}