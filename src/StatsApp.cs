using S1API.PhoneApp;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

namespace ScheduleIStats;

public sealed class StatsApp : PhoneApp
{
    public static StatsApp? Current { get; private set; }
    protected override string AppName => "ScheduleIStats";
    protected override string AppTitle => "CS SAS";
    protected override string IconLabel => "CS SAS";
    protected override string IconFileName => "";
    protected override EOrientation Orientation => EOrientation.Horizontal;
    private static Sprite? icon, rounded, gear;
    private Image? settingsIcon;
    protected override Sprite? IconSprite => icon ??= CreateIcon();
    private static readonly Color Ink = new(.90f, .94f, .94f);
    private static readonly Color Muted = new(.52f, .64f, .66f);
    private static readonly Color Mint = new(.42f, .91f, .72f);
    private static readonly Color CardColor = new(.075f, .115f, .13f);
    private GameObject? root, filter;
    private Text? status, caption, footer, filterText;
    private RectTransform? content, viewport;
    private ScrollRect? scroll;
    private readonly List<(Image Background, Text Label)> tabs = new();
    private readonly List<CardUI> cards = new();
    private int tab, historyLimit = 40;
    private bool today, resetScroll = true;
    private string renderedKey = "";

    private sealed class CardData
    {
        public string Kicker = "", Title = "", Detail = "";
        public bool Hero;
        public List<(string Label, string Value)> Rows = new();
    }
    private sealed class CardUI
    {
        public GameObject Root = null!;
        public Text Kicker = null!, Title = null!, Detail = null!;
        public readonly List<(GameObject Root, Text Label, Text Value)> Rows = new();
    }

    protected override void OnCreatedUI(GameObject container)
    {
        Current = this;
        root = UIFactory.Panel("Cravel", container.transform, new Color(.035f, .055f, .065f), fullAnchor: true);
        var brand = Text("Marca", "CS SAS", root.transform, 30, Ink, FontStyle.Bold);
        Place(brand.rectTransform, .045f, .865f, .40f, .975f);
        var tagline = Text("Subtitulo", "TU NEGOCIO, EN UN VISTAZO", root.transform, 10, Muted);
        Place(tagline.rectTransform, .048f, .825f, .65f, .87f);
        var badge = Panel("Estado", root.transform, new Color(.08f, .19f, .16f), true);
        Place(badge.GetComponent<RectTransform>(), .70f, .895f, .955f, .958f);
        status = Text("EstadoTexto", "CONECTANDO", badge.transform, 11, Mint, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(status.rectTransform, 0, 0, 1, 1);
        caption = Text("EstadoDetalle", "Esperando la partida…", root.transform, 12, Muted);
        Place(caption.rectTransform, .048f, .777f, .955f, .825f);
        string[] labels = { "Resumen", "Productos", "Historial", "Stock", "" };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var pill = Button(root.transform, labels[i], () => SelectTab(index));
            if (i < 4) Place(pill.Background.rectTransform, .045f + i * .205f, .665f, .240f + i * .205f, .755f);
            else
            {
                Place(pill.Background.rectTransform, .884f, .665f, .955f, .755f);
                var go = new GameObject("Ajustes"); go.transform.SetParent(pill.Background.transform, false);
                var rt = go.AddComponent<RectTransform>(); Place(rt, .18f, .18f, .82f, .82f);
                settingsIcon = go.AddComponent<Image>(); settingsIcon.sprite = gear ??= CreateGear();
                settingsIcon.preserveAspect = true; settingsIcon.raycastTarget = false;
            }
            tabs.Add(pill);
        }
        var scrollObject = new GameObject("ListaConScroll");
        scrollObject.transform.SetParent(root.transform, false);
        var scrollRect = scrollObject.AddComponent<RectTransform>();
        Place(scrollRect, .04f, .125f, .96f, .642f);
        scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = .12f;
        scroll.scrollSensitivity = 24;
        var view = Panel("VentanaDeLista", scrollObject.transform, new Color(1, 1, 1, .005f));
        viewport = view.GetComponent<RectTransform>();
        Place(viewport, 0, 0, .975f, 1);
        view.AddComponent<RectMask2D>();
        content = new GameObject("Contenido").AddComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 10);
        layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        AddScrollbar(scrollObject.transform);
        footer = Text("Pie", "VISTA LOCAL · 0.1.8", root.transform, 10, Muted);
        Place(footer.rectTransform, .05f, .035f, .66f, .10f);
        var filterPill = Button(root.transform, "Todo el historial", () => { today = !today; resetScroll = true; renderedKey = ""; Refresh(); });
        filter = filterPill.Background.gameObject; filterText = filterPill.Label;
        Place(filterPill.Background.rectTransform, .68f, .028f, .955f, .107f);
        Refresh();
    }

    private void SelectTab(int index)
    {
        if (index == tab) return;
        tab = index; historyLimit = 40; resetScroll = true; renderedKey = ""; Refresh();
    }

    public void Refresh()
    {
        if (root == null || !root.activeInHierarchy || scroll == null || content == null || viewport == null || status == null) return;
        var mod = StatsMod.Instance;
        if (mod == null) return;
        var data = mod.Data;
        status.text = mod.Host ? "ANFITRIÓN" : data != null && mod.Status.StartsWith("Sincronizado", StringComparison.Ordinal) ? "SINCRONIZADO" : "CONECTANDO";
        caption!.text = mod.Status;
        filter!.SetActive(tab <= 2);
        filterText!.text = today ? "Solo hoy" : "Todo el historial";
        footer!.text = tab == 3 ? (mod.Stock == null ? "ESPERANDO STOCK" : $"ÚLTIMA LECTURA · {mod.Stock.CheckedUtc.ToLocalTime():HH:mm:ss}") : "CS SAS · VISTA LOCAL 0.1.8";
        int day = Il2CppScheduleOne.GameTime.TimeManager.InstanceExists ? Il2CppScheduleOne.GameTime.TimeManager.Instance.ElapsedDays : 0;
        if (tab == 2 && data != null && scroll.verticalNormalizedPosition < .12f &&
            historyLimit < (today ? data.Sales.Count(s => s.Day == day) : data.Sales.Count))
            historyLimit += 40;
        string key = $"{tab}|{today}|{day}|{data?.Id}|{data?.Name}|{data?.Sales.Count}|{mod.Stock?.Revision}|{mod.Stock?.CashRevision}|{historyLimit}";
        if (data == null || tab == 4) key += $"|{mod.Status}|{mod.NetworkStatus}|{mod.SyncSent}|{mod.SyncReceived}|{mod.CaptureStatus}|{mod.DeliveriesObserved}";
        if (tab == 3 && mod.Stock == null) key += mod.StockStatus;
        if (key == renderedKey) return;
        renderedKey = key;
        for (int i = 0; i < tabs.Count; i++)
        {
            tabs[i].Background.color = i == tab ? Mint : CardColor;
            tabs[i].Label.color = i == tab ? new Color(.04f, .14f, .11f) : Muted;
            tabs[i].Label.fontStyle = i == tab ? FontStyle.Bold : FontStyle.Normal;
        }
        if (settingsIcon != null) settingsIcon.color = tab == 4 ? new Color(.04f, .14f, .11f) : Muted;
        var models = BuildCards(mod, day);
        float offset = content.anchoredPosition.y;
        for (int i = 0; i < models.Count; i++)
        {
            if (i >= cards.Count) cards.Add(CreateCard(content));
            UpdateCard(cards[i], models[i]);
        }
        for (int i = models.Count; i < cards.Count; i++) cards[i].Root.SetActive(false);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scroll.StopMovement();
        float range = Mathf.Max(0, content.rect.height - viewport.rect.height);
        scroll.verticalNormalizedPosition = resetScroll || range <= 0 ? 1 : 1 - Mathf.Clamp(offset, 0, range) / range;
        resetScroll = false;
    }

    private List<CardData> BuildCards(StatsMod mod, int day)
    {
        var result = new List<CardData>();
        var data = mod.Data;
        if (tab == 4)
        {
            result.Add(new CardData { Kicker = "CONEXIÓN", Title = mod.Host ? "Registro principal" : "Tu copia compartida", Detail = mod.Status,
                Rows = new() { ("Solicitudes enviadas", mod.SyncSent.ToString("N0")), ("Respuestas recibidas", mod.SyncReceived.ToString("N0")), ("Ventas guardadas", (data?.Sales.Count ?? 0).ToString("N0")) } });
            result.Add(new CardData { Kicker = "ESTADO DE LA PARTIDA", Title = mod.NetworkStatus, Detail = mod.CaptureStatus,
                Rows = new() { ("Entregas observadas", mod.DeliveriesObserved.ToString("N0")) } });
            result.Add(new CardData { Kicker = "VERSIÓN LOCAL", Title = "CS SAS 0.1.8", Detail = "Diseño en prueba. Compatible con la sincronización de 0.1.6. El registro de ventas empieza con la instalación del mod." });
            return result;
        }
        if (data == null)
        {
            result.Add(new CardData { Kicker = "CONECTANDO", Title = "Preparando tus datos", Detail = mod.Status + "\n" + mod.NetworkStatus });
            return result;
        }
        var sales = data.Sales.Where(s => !today || s.Day == day).ToList();
        string period = today ? $"DÍA {day + 1}" : "TODO EL HISTORIAL";
        if (tab == 0)
        {
            result.Add(new CardData { Kicker = period + " · INGRESOS", Title = DisplayFormat.Money(sales.Sum(s => s.Total)), Hero = true,
                Detail = data.Name, Rows = new() { ("Ventas realizadas", sales.Count.ToString("N0")), ("Unidades vendidas", sales.Sum(s => s.Lines.Sum(l => l.Quantity)).ToString("N0")) } });
            AddInventoryCards(result, mod, true);
            result.Add(new CardData { Kicker = "TU REGISTRO", Title = "Cada venta cuenta", Detail = $"Registrando desde el {data.StartedUtc.ToLocalTime():dd/MM/yyyy HH:mm}. Los ingresos no descuentan costos." });
        }
        else if (tab == 1)
        {
            var products = sales.SelectMany(s => s.Lines.Select(l => new { Line = l, Sale = s }))
                .GroupBy(x => x.Line.ProductId).OrderByDescending(g => g.Sum(x => x.Line.Quantity));
            foreach (var g in products)
            {
                long qty = g.Sum(x => (long)x.Line.Quantity);
                decimal revenue = g.Sum(x => x.Sale.Total * x.Line.Quantity / x.Sale.Lines.Sum(l => l.Quantity));
                bool estimated = g.Any(x => x.Sale.Lines.Count > 1);
                result.Add(new CardData { Kicker = period, Title = g.First().Line.Product,
                    Detail = estimated ? "Precio estimado: incluye ventas de varios productos." : "Precio medio de las ventas registradas.",
                    Rows = new() { ("Unidades vendidas", qty.ToString("N0")), ("Precio por unidad", DisplayFormat.Money(revenue / qty)) } });
            }
        }
        else if (tab == 2)
        {
            foreach (var sale in sales.AsEnumerable().Reverse().Take(historyLimit))
                result.Add(new CardData { Kicker = $"DÍA {sale.Day + 1} · {sale.Time / 100:00}:{sale.Time % 100:00}", Title = sale.Customer,
                    Detail = "Venta completada", Rows = sale.Lines.Select(l => (l.Product, $"{l.Quantity:N0} u.")).Append(("TOTAL", DisplayFormat.Money(sale.Total))).ToList() });
        }
        else
        {
            AddInventoryCards(result, mod, false);
        }
        if (result.Count == 0) result.Add(new CardData { Kicker = tab == 3 ? "INVENTARIO" : period, Title = tab == 3 ? "Todavía no hay stock" : "Todavía no hay ventas", Detail = tab == 3 ? "Los productos aparecerán al consultar los contenedores y jugadores." : "Las próximas ventas completadas aparecerán acá." });
        return result;
    }

    private static void AddInventoryCards(List<CardData> result, StatsMod mod, bool summary)
    {
        var stock = mod.Stock;
        if (stock == null)
        {
            result.Add(new CardData { Kicker = "INVENTARIO", Title = "Consultando inventario", Detail = mod.Host ? mod.StockStatus : mod.Status });
            return;
        }
        var groups = StockPlaces.Groups(stock.Lines);
        if (summary && !groups.Any(g => g.Total > 0))
            result.Add(new CardData { Kicker = "AHORA · INVENTARIO", Title = "Sin inventario", Detail = "No hay productos en los lugares consultados." });
        foreach (var place in groups.Where(g => !summary || g.Total > 0))
        {
            bool player = place.Place.StartsWith("Jugador: ", StringComparison.Ordinal);
            bool dealer = place.Place.StartsWith("Camello: ", StringComparison.Ordinal);
            var cash = stock.DealerCash?.FirstOrDefault(d => d.Location == place.Place);
            var rows = place.Items.Select(l => (l.Product, $"{l.Quantity:N0} u.")).ToList();
            if (cash != null) rows.Add(("Dinero por cobrar", DisplayFormat.Money(cash.Cash)));
            result.Add(new CardData { Kicker = (summary ? "INVENTARIO · " : "") + (player ? "JUGADOR" : dealer ? "CAMELLO" : "PROPIEDAD"),
                Title = player || dealer ? place.Place[9..] : place.Place,
                Detail = place.Total == 0 ? "Sin inventario" : $"{place.Total:N0} unidades",
                Rows = rows });
        }
        if (summary && stock.DealerCash != null)
        {
            var pending = stock.DealerCash.Where(d => d.Cash > 0 && !groups.Any(g => g.Place == d.Location && g.Total > 0)).ToList();
            if (pending.Count > 0) result.Add(new CardData { Kicker = "CAMELLOS", Title = "Dinero por cobrar",
                Detail = "Efectivo que conservan los camellos sin productos.", Rows = pending.Select(d => (d.Name, DisplayFormat.Money(d.Cash))).ToList() });
        }
        if (!summary && groups.Count == 0) result.Add(new CardData { Kicker = "INVENTARIO", Title = "Sin inventario", Detail = "No hay productos en los lugares consultados." });
    }

    private static CardUI CreateCard(Transform parent)
    {
        var go = Panel("Tarjeta", parent, CardColor, true);
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14); layout.spacing = 6;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        var card = new CardUI { Root = go, Kicker = Text("Etiqueta", "", go.transform, 10, Mint, FontStyle.Bold),
            Title = Text("Titulo", "", go.transform, 22, Ink, FontStyle.Bold), Detail = Text("Detalle", "", go.transform, 12, Muted) };
        return card;
    }
    private static void UpdateCard(CardUI ui, CardData model)
    {
        ui.Root.SetActive(true);
        ui.Root.GetComponent<Image>().color = model.Hero ? new Color(.07f, .18f, .15f) : CardColor;
        ui.Kicker.text = model.Kicker; ui.Title.text = model.Title; ui.Title.fontSize = model.Hero ? 38 : 22;
        ui.Detail.text = model.Detail; ui.Detail.gameObject.SetActive(model.Detail.Length > 0);
        for (int i = 0; i < model.Rows.Count; i++)
        {
            if (i >= ui.Rows.Count)
            {
                var row = new GameObject("Fila"); row.transform.SetParent(ui.Root.transform, false);
                row.AddComponent<RectTransform>();
                row.AddComponent<LayoutElement>().preferredHeight = 32;
                var label = Text("Nombre", "", row.transform, 14, Ink, alignment: TextAnchor.MiddleLeft);
                var value = Text("Cantidad", "", row.transform, 16, Mint, FontStyle.Bold, TextAnchor.MiddleRight);
                Place(label.rectTransform, 0, 0, .69f, 1); Place(value.rectTransform, .70f, 0, 1, 1);
                label.verticalOverflow = VerticalWrapMode.Truncate; value.verticalOverflow = VerticalWrapMode.Truncate;
                ui.Rows.Add((row, label, value));
            }
            ui.Rows[i].Root.SetActive(true); ui.Rows[i].Label.text = model.Rows[i].Label; ui.Rows[i].Value.text = model.Rows[i].Value;
        }
        for (int i = model.Rows.Count; i < ui.Rows.Count; i++) ui.Rows[i].Root.SetActive(false);
    }
    private static Text Text(string name, string value, Transform parent, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var text = UIFactory.Text(name, value, parent, size, alignment, style);
        text.color = color; text.supportRichText = false; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
    private static GameObject Panel(string name, Transform parent, Color color, bool round = false)
    {
        var panel = UIFactory.Panel(name, parent, color);
        if (round) { var image = panel.GetComponent<Image>(); image.sprite = rounded ??= CreateRounded(); image.type = Image.Type.Sliced; }
        return panel;
    }
    private static (Image Background, Text Label) Button(Transform parent, string label, Action onClick)
    {
        var panel = Panel(label, parent, CardColor, true);
        var image = panel.GetComponent<Image>();
        var button = panel.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener((UnityEngine.Events.UnityAction)onClick);
        var text = Text("Texto", label, panel.transform, 13, Muted, alignment: TextAnchor.MiddleCenter);
        Place(text.rectTransform, .02f, 0, .98f, 1);
        return (image, text);
    }
    private static void Place(RectTransform rt, float x1, float y1, float x2, float y2)
    {
        rt.anchorMin = new Vector2(x1, y1); rt.anchorMax = new Vector2(x2, y2);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    private void AddScrollbar(Transform parent)
    {
        var track = Panel("BarraDeScroll", parent, new Color(.10f, .17f, .18f), true);
        Place(track.GetComponent<RectTransform>(), .986f, .01f, .996f, .99f);
        var handle = Panel("Indicador", track.transform, new Color(.30f, .51f, .46f), true);
        var handleRect = handle.GetComponent<RectTransform>(); Place(handleRect, 0, 0, 1, 1);
        var bar = track.AddComponent<Scrollbar>(); bar.handleRect = handleRect;
        bar.targetGraphic = handle.GetComponent<Image>(); bar.direction = Scrollbar.Direction.BottomToTop;
        scroll!.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }
    private static Sprite CreateRounded()
    {
        const int size = 40, radius = 10;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(radius - x - .5f, x + .5f - (size - radius));
            float dy = Mathf.Max(radius - y - .5f, y + .5f - (size - radius));
            float alpha = dx > 0 && dy > 0 ? Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f) : 1;
            texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
    private static Sprite CreateGear()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = x + .5f - 32, dy = y + .5f - 32;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            float teeth = Mathf.Cos(Mathf.Atan2(dy, dx) * 10);
            float edge = teeth > .12f ? 27 : 22;
            float alpha = Mathf.Clamp01(edge - radius) * Mathf.Clamp01(radius - 11);
            texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
    }
    private static Sprite CreateIcon()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var origin = new Vector2(32, 26);
        var leaves = new List<Vector2[]>();
        var tips = new[] { new Vector2(32, 58), new Vector2(17, 52), new Vector2(47, 52),
            new Vector2(6, 39), new Vector2(58, 39), new Vector2(13, 22), new Vector2(51, 22) };
        float[] widths = { 8, 7.2f, 7.2f, 6.5f, 6.5f, 4.7f, 4.7f };
        Vector2[] edge = { new(0, 0), new(-.25f, .10f), new(-.40f, .08f), new(-.32f, .24f),
            new(-.62f, .21f), new(-.45f, .39f), new(-.78f, .35f), new(-.52f, .56f),
            new(-.70f, .52f), new(-.39f, .74f), new(-.46f, .70f), new(-.19f, .89f), new(0, 1),
            new(.19f, .89f), new(.46f, .70f), new(.39f, .74f), new(.70f, .52f), new(.52f, .56f),
            new(.78f, .35f), new(.45f, .39f), new(.62f, .21f), new(.32f, .24f), new(.40f, .08f), new(.25f, .10f) };
        for (int i = 0; i < tips.Length; i++)
        {
            var direction = tips[i] - origin;
            var side = new Vector2(direction.y, -direction.x).normalized;
            leaves.Add(edge.Select(p => origin + direction * p.y + side * (p.x * widths[i])).ToArray());
        }
        bool Leaf(Vector2 p) => leaves.Any(poly => Inside(p, poly)) || (p.x > 30.8f && p.x < 33.2f && p.y > 10 && p.y < 29);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            var p = new Vector2((x + .5f) / 2, (y + .5f) / 2);
            float dx = Mathf.Max(10 - p.x, p.x - 54), dy = Mathf.Max(10 - p.y, p.y - 54);
            bool outside = dx > 0 && dy > 0 && dx * dx + dy * dy > 100;
            Color color = outside ? Color.clear : new Color(.18f, .31f, .23f);
            if (!outside && Vector2.Distance(p, new Vector2(32, 33)) < 28) color = new Color(.91f, .88f, .69f);
            if (!outside)
            {
                bool fill = Leaf(p);
                bool outline = !fill && (Leaf(p + Vector2.right) || Leaf(p - Vector2.right) || Leaf(p + Vector2.up) || Leaf(p - Vector2.up));
                if (outline) color = new Color(.07f, .20f, .10f);
                if (fill) color = Color.Lerp(new Color(.18f, .43f, .20f), new Color(.39f, .65f, .26f), p.y / 64);
                if (fill && tips.Any(t => SegmentDistance(p, origin, t) < .28f)) color = new Color(.58f, .73f, .34f);
            }
            texture.SetPixel(x, y, color);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
    }
    private static bool Inside(Vector2 p, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                inside = !inside;
        return inside;
    }
    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var segment = b - a;
        return Vector2.Distance(p, a + Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude) * segment);
    }
}


