using S1API.PhoneApp;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleIStats;

public sealed class StatsApp : PhoneApp
{
    public static StatsApp? Current { get; private set; }
    protected override string AppName => "ScheduleIStats";
    protected override string AppTitle => "Cravel";
    protected override string IconLabel => "Cravel";
    protected override string IconFileName => "";
    protected override EOrientation Orientation => EOrientation.Horizontal;
    private static Sprite? icon;
    protected override Sprite? IconSprite => icon ??= CreateIcon();
    private Text? body, status, pageLabel;
    private int tab, page;
    private bool today;
    private static string Money(decimal n) => DisplayFormat.Money(n);

    protected override void OnCreatedUI(GameObject container)
    {
        Current = this;
        var root = UIFactory.Panel("Estadisticas", container.transform, new Color(.055f, .075f, .09f), fullAnchor: true);
        var title = UIFactory.Text("Titulo", "CRAVEL  /  TU NEGOCIO", root.transform, 22, TextAnchor.MiddleLeft, FontStyle.Bold);
        Place(title.rectTransform, .04f, .84f, .96f, .95f);
        title.color = new Color(.40f, .89f, .72f);
        status = UIFactory.Text("Estado", "Cargando…", root.transform, 13);
        Place(status.rectTransform, .04f, .77f, .96f, .84f);
        string[] tabs = { "Resumen", "Productos", "Historial", "Stock", "Sincronización" };
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            Button(root.transform, tabs[i], .04f + i * .184f, .65f, .213f + i * .184f, .75f,
                () => { tab = index; page = 0; Refresh(); });
        }
        body = UIFactory.Text("Contenido", "", root.transform, 16);
        Place(body.rectTransform, .045f, .16f, .955f, .62f);
        body.supportRichText = false;
        body.verticalOverflow = VerticalWrapMode.Truncate;
        body.color = new Color(.91f, .93f, .94f);
        Button(root.transform, "Anterior", .04f, .035f, .22f, .13f, () => { page = Math.Max(0, page - 1); Refresh(); });
        Button(root.transform, "Siguiente", .24f, .035f, .42f, .13f, () => { page++; Refresh(); });
        Button(root.transform, "Hoy / Todo", .65f, .035f, .96f, .13f, () => { today = !today; page = 0; Refresh(); });
        pageLabel = UIFactory.Text("Pagina", "", root.transform, 13, TextAnchor.MiddleCenter);
        Place(pageLabel.rectTransform, .43f, .035f, .64f, .13f);
        Refresh();
    }

    private static void Place(RectTransform rt, float x1, float y1, float x2, float y2)
    {
        rt.anchorMin = new Vector2(x1, y1); rt.anchorMax = new Vector2(x2, y2);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    private static void Button(Transform parent, string label, float x1, float y1, float x2, float y2, Action click)
    {
        var panel = UIFactory.Panel(label, parent, new Color(.12f, .20f, .23f));
        Place(panel.GetComponent<RectTransform>(), x1, y1, x2, y2);
        var b = panel.AddComponent<Button>(); b.targetGraphic = panel.GetComponent<Image>();
        b.onClick.AddListener((UnityEngine.Events.UnityAction)click);
        var text = UIFactory.Text("Texto", label, panel.transform, 14, TextAnchor.MiddleCenter);
        Place(text.rectTransform, 0, 0, 1, 1); text.raycastTarget = false;
    }

    public void Refresh()
    {
        if (body == null || status == null || pageLabel == null) return;
        var mod = StatsMod.Instance;
        if (mod == null) return;
        status.text = mod.Status;
        var data = mod.Data;
        if (data == null)
        {
            body.text = $"{mod.Status}\n{mod.NetworkStatus}\nEnviados: {mod.SyncSent} · Recibidos: {mod.SyncReceived}\n{mod.CaptureStatus}\n\nAmbas PCs deben tener la versión 0.1.4.\nLas ventas se registran desde la instalación.";
            return;
        }
        int day = Il2CppScheduleOne.GameTime.TimeManager.InstanceExists ? Il2CppScheduleOne.GameTime.TimeManager.Instance.ElapsedDays : 0;
        var sales = data.Sales.Where(s => !today || s.Day == day).ToList();
        string period = today ? $"Día {day + 1}" : "Todo el historial";
        if (tab == 0)
        {
            body.text = $"{period} · {data.Name}\n\nIngresos: {Money(sales.Sum(s => s.Total))}\nVentas: {sales.Count:N0}     Unidades: {sales.Sum(s => s.Lines.Sum(l => l.Quantity)):N0}\n\nRegistro iniciado: {data.StartedUtc.ToLocalTime():dd/MM/yyyy HH:mm}\nLos importes son ingresos; no descuentan costos.";
            page = 0;
        }
        else if (tab == 1)
        {
            var products = sales.SelectMany(s => s.Lines.Select(l => new { Line = l, Sale = s }))
                .GroupBy(x => x.Line.ProductId).OrderByDescending(g => g.Sum(x => x.Line.Quantity)).ToList();
            page = Math.Clamp(page, 0, Math.Max(0, (products.Count - 1) / 4));
            body.text = period + " · Producto / unidades / precio medio\n\n" + string.Join("\n", products.Skip(page * 4).Take(4).Select(g => {
                int qty = g.Sum(x => x.Line.Quantity);
                decimal revenue = g.Sum(x => x.Sale.Total * x.Line.Quantity / x.Sale.Lines.Sum(l => l.Quantity));
                bool estimated = g.Any(x => x.Sale.Lines.Count > 1);
                return $"{g.First().Line.Product} · {qty} u. · {Money(revenue / qty)}{(estimated ? " *" : "")}";
            })) + (products.Count == 0 ? "Todavía no hay ventas registradas." : "\n\n* Estimado: ventas con varios productos, repartidas por unidad.");
        }
        else if (tab == 2)
        {
            page = Math.Clamp(page, 0, Math.Max(0, (sales.Count - 1) / 3));
            body.text = period + " · Últimas ventas primero\n\n" + string.Join("\n\n", sales.AsEnumerable().Reverse().Skip(page * 3).Take(3).Select(s =>
                $"D{s.Day + 1} {s.Time / 100:00}:{s.Time % 100:00} · {s.Customer} · {Money(s.Total)}\n" +
                string.Join(", ", s.Lines.Select(l => $"{l.Quantity} × {l.Product}")))) + (sales.Count == 0 ? "Todavía no hay ventas registradas." : "");
        }
        else if (tab == 3)
        {
            var stock = mod.Stock;
            if (stock == null)
            {
                body.text = "Stock pendiente de actualización.\n" + (mod.Host ? mod.StockStatus : mod.Status) +
                    "\n\nProductos terminados en contenedores de propiedades propias.\nNo incluye bolsillos ni producción en curso.";
                page = 0;
            }
            else
            {
                var lines = stock.Lines.OrderBy(l => l.Product, StringComparer.Ordinal).ThenBy(l => l.Location, StringComparer.Ordinal).ToList();
                page = Math.Clamp(page, 0, Math.Max(0, (lines.Count - 1) / 3));
                body.text = $"Stock actual · {lines.Sum(l => l.Quantity):N0} unidades\nÚltima lectura: {stock.CheckedUtc.ToLocalTime():HH:mm:ss}\n\n" +
                    (lines.Count == 0 ? "Sin productos en los contenedores consultados." :
                    string.Join("\n", lines.Skip(page * 3).Take(3).Select(l => $"{l.Product} · {l.Quantity:N0} u.\n{l.Location}"))) +
                    "\n\nPropiedades propias · sin bolsillos ni producción en curso.";
            }
        }
        else
        {
            body.text = $"{(mod.Host ? "Anfitrión · registro principal" : "Invitado · copia del anfitrión")}\n{mod.NetworkStatus}\nEnviados: {mod.SyncSent} · Recibidos: {mod.SyncReceived}\nVentas: {data.Sales.Count} · Entregas observadas: {mod.DeliveriesObserved}\n{mod.CaptureStatus}\n\nVersión 0.1.4 · Ambas PCs deben actualizar.\nNo importa ventas anteriores a la instalación.";
            page = 0;
        }
        pageLabel.text = $"Pág. {page + 1}";
    }

    private static Sprite CreateIcon()
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            bool bar = (x >= 12 && x < 22 && y >= 13 && y < 29) || (x >= 27 && x < 37 && y >= 13 && y < 40) || (x >= 42 && x < 52 && y >= 13 && y < 52);
            texture.SetPixel(x, y, bar ? new Color(.4f, .89f, .72f) : new Color(.055f, .075f, .09f));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
    }
}




