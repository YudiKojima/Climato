const formatter = new Intl.DateTimeFormat("pt-BR", {
  weekday: "short",
  day: "2-digit",
  month: "2-digit",
  timeZone: "UTC",
});

export const formatWeekday = (isoDate) => formatter.format(new Date(`${isoDate}T00:00:00Z`));
