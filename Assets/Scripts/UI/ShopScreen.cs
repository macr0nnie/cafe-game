using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The shop computer's screen, built into a menu board: a coins line, two tabs
/// (Upgrades, Play together) and a two-column list of upgrades with a buy button
/// each. Every state is written out in words, never shown by colour alone.
public class ShopScreen
{
    class Row { public UpgradeDef Def; public Button Buy; public Text BuyLabel; public Text Status; }

    readonly List<Row> _rows = new List<Row>();
    readonly Text _coins;
    readonly RectTransform _upgradesTab, _friendsTab;
    readonly Text _upgradesTabLabel, _friendsTabLabel;

    public Selectable First { get; private set; }

    public ShopScreen(RectTransform board, UnityEngine.Events.UnityAction close)
    {
        _coins = CafeUIKit.Label(board, "", 28, CafeUIKit.Ink);
        CafeUIKit.Size(_coins, -1f, 36f);

        // Tabs.
        var tabs = Row_(board, "Tabs", 60f, 24f);
        var upBtn = CafeUIKit.Button(tabs, "Upgrades", CafeUIKit.Mint, () => ShowTab(true), 56f, 26);
        var frBtn = CafeUIKit.Button(tabs, "Play together", CafeUIKit.Lavender, () => ShowTab(false), 56f, 26);
        CafeUIKit.Size(upBtn, 300f, 56f); CafeUIKit.Size(frBtn, 300f, 56f);
        SetHorizontal(upBtn); SetHorizontal(frBtn);
        _upgradesTabLabel = upBtn.GetComponentInChildren<Text>();
        _friendsTabLabel = frBtn.GetComponentInChildren<Text>();
        First = upBtn;

        // Upgrades: two columns like the recipe book.
        _upgradesTab = CafeUIKit.Node(board, "UpgradesTab");
        CafeUIKit.Size(_upgradesTab).flexibleHeight = 1f;
        var hz = _upgradesTab.gameObject.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = 32f; hz.childControlWidth = true; hz.childControlHeight = true;
        hz.childForceExpandWidth = true; hz.childForceExpandHeight = true;

        var all = CafeUpgrades.Catalog != null ? CafeUpgrades.Catalog.All : new UpgradeDef[0];
        int half = (all.Length + 1) / 2;
        for (int c = 0; c < 2; c++)
        {
            var col = CafeUIKit.Node(_upgradesTab, "Col" + c);
            CafeUIKit.Stack(col.gameObject, 0, 0, 10f, TextAnchor.UpperLeft);
            for (int i = c * half; i < Mathf.Min(all.Length, (c + 1) * half); i++)
                if (all[i] != null) _rows.Add(BuildRow(col, all[i]));
        }
        if (all.Length == 0)
            CafeUIKit.Size(CafeUIKit.Label(_upgradesTab, "Nothing for sale yet.", 26, CafeUIKit.InkSoft), -1f, 40f);

        // Play together: placeholder until networking lands.
        _friendsTab = CafeUIKit.Node(board, "FriendsTab");
        CafeUIKit.Size(_friendsTab).flexibleHeight = 1f;
        CafeUIKit.Stack(_friendsTab.gameObject, 40, 20, 16f);
        CafeUIKit.Size(CafeUIKit.Label(_friendsTab, "Play together", 40, CafeUIKit.Ink, TextAnchor.MiddleCenter, true), -1f, 56f);
        CafeUIKit.Size(CafeUIKit.Label(_friendsTab,
            "Soon you will be able to open your cafe to a friend and work the counter together:\n" +
            "one on the espresso machine, one taking orders.\n\nThis is not ready yet. Nothing here connects to the internet.",
            26, CafeUIKit.InkSoft), -1f, 200f);
        var host = CafeUIKit.Button(_friendsTab, "Host a cafe  (coming soon)", CafeUIKit.Butter, null);
        host.interactable = false;

        var closeRow = Row_(board, "Buttons", 64f, 0f);
        var closeBtn = CafeUIKit.Button(closeRow, "Back to the cafe", CafeUIKit.Pink, close);
        CafeUIKit.Size(closeBtn, 340f, 64f);

        CafeUpgrades.Progress.Changed += Refresh;
        ShowTab(true);
    }

    Row BuildRow(Transform col, UpgradeDef u)
    {
        var card = CafeUIKit.Card(col, "Upgrade_" + u.Id, CafeUIKit.Blush);
        CafeUIKit.Size(card, -1f, 104f);
        var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(22, 18, 10, 10); h.spacing = 16f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        var words = CafeUIKit.Node(card.transform, "Words");
        CafeUIKit.Stack(words.gameObject, 0, 0, 2f, TextAnchor.MiddleLeft);
        CafeUIKit.Size(words).flexibleWidth = 1f;
        CafeUIKit.Size(CafeUIKit.Label(words, u.Title, 28, CafeUIKit.Ink, TextAnchor.LowerLeft, true), -1f, 32f);
        CafeUIKit.Size(CafeUIKit.Label(words, u.Description, 20, CafeUIKit.Ink, TextAnchor.UpperLeft), -1f, 24f);
        var status = CafeUIKit.Label(words, "", 20, CafeUIKit.InkSoft, TextAnchor.UpperLeft);
        CafeUIKit.Size(status, -1f, 24f);

        var row = new Row { Def = u, Status = status };
        row.Buy = CafeUIKit.Button(card.transform, "", CafeUIKit.Mint, () => Buy(row), 56f, 24);
        CafeUIKit.Size(row.Buy, 190f, 56f);
        row.BuyLabel = row.Buy.GetComponentInChildren<Text>();
        var nav = row.Buy.navigation; nav.mode = Navigation.Mode.Automatic; row.Buy.navigation = nav;
        return row;
    }

    void Buy(Row r)
    {
        if (CafeUpgrades.Progress.TryBuy(r.Def))
            CafeMessage.Show(r.Def.Title + " installed - " + CafeUpgrades.Effect(r.Def.Target).ToLower(), 3f);
    }

    void ShowTab(bool upgrades)
    {
        _upgradesTab.gameObject.SetActive(upgrades);
        _friendsTab.gameObject.SetActive(!upgrades);
        // The open tab is marked in words as well as by what shows below it.
        _upgradesTabLabel.text = upgrades ? "~ Upgrades ~" : "Upgrades";
        _friendsTabLabel.text = upgrades ? "Play together" : "~ Play together ~";
        Refresh();
    }

    public void Refresh()
    {
        // The menus were destroyed (scene reload): stop listening.
        if (_coins == null) { CafeUpgrades.Progress.Changed -= Refresh; return; }
        var p = CafeUpgrades.Progress;
        _coins.text = "Tip jar  <b>" + p.Coins + " coins</b>";
        foreach (var r in _rows)
        {
            string why = p.WhyNot(r.Def);
            bool owned = p.Owns(r.Def);
            r.Buy.interactable = why == null;
            r.BuyLabel.text = owned ? "Installed" : "Buy  " + r.Def.Cost;
            r.Status.text = CafeUpgrades.Effect(r.Def.Target) + "  -  "
                          + (owned ? "Installed" : (why ?? "Ready to buy"));
        }
    }

    static RectTransform Row_(Transform parent, string name, float height, float spacing)
    {
        var row = CafeUIKit.Node(parent, name);
        CafeUIKit.Size(row, -1f, height);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing; h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false;
        return row;
    }

    static void SetHorizontal(Button b)
    {
        var nav = b.navigation; nav.mode = Navigation.Mode.Automatic; b.navigation = nav;
    }
}
