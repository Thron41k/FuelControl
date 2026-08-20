window.fuelControl = window.fuelControl || {};

window.fuelControl.getTimeZoneId = function () {
    return Intl.DateTimeFormat()
        .resolvedOptions()
        .timeZone;
};

window.fuelControl.getTimeZone = function () {
    const timeZone =
        Intl.DateTimeFormat().resolvedOptions().timeZone;

    const year = new Date().getFullYear();

    /*
     * getTimezoneOffset() возвращает значение наоборот:
     *
     * UTC+9 -> -540
     *
     * Omnicomm ожидает:
     *
     * UTC+9 -> 540
     */

    const winterDate =
        new Date(Date.UTC(year, 0, 1, 12, 0, 0));

    const summerDate =
        new Date(Date.UTC(year, 6, 1, 12, 0, 0));

    const winterOffset =
        getOffsetForDate(winterDate, timeZone);

    const summerOffset =
        getOffsetForDate(summerDate, timeZone);

    return {
        timeZone: timeZone,
        winterOffset: winterOffset,
        summerOffset: summerOffset
    };
};


function getOffsetForDate(date, timeZone) {
    const parts =
        new Intl.DateTimeFormat(
            "en-US",
            {
                timeZone: timeZone,
                timeZoneName: "shortOffset"
            })
            .formatToParts(date);

    const timeZoneName =
        parts.find(
            x => x.type === "timeZoneName");

    if (!timeZoneName) {
        return 0;
    }

    const value =
        timeZoneName.value;

    /*
     * Форматы:
     *
     * GMT+9
     * GMT+09:00
     * GMT+1
     * GMT+01:00
     */

    const match =
        value.match(
            /^GMT([+-])(\d{1,2})(?::(\d{2}))?$/);

    if (!match) {
        return 0;
    }

    const sign =
        match[1] === "-"
            ? -1
            : 1;

    const hours =
        Number(match[2]);

    const minutes =
        Number(match[3] ?? 0);

    return sign * (hours * 60 + minutes);
}