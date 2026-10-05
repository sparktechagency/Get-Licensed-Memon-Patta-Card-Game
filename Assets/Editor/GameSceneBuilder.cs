using System;
using MemonPatta.Net;
using MemonPatta.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Builds the whole GamePlay scene UI (portrait 1080x1920): name, lobby, room, game table, round summary.
// Re-running it replaces the previously generated objects.
public static class GameSceneBuilder
{
    const string ScenePath = "Assets/Scenes/GamePlay.unity";
    const string CardsFolder = "Assets/UI Elements/PNG-cards-1.3/";

    static readonly Color Felt = new Color(0.05f, 0.27f, 0.17f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.4f);
    static readonly Color Green = new Color(0.16f, 0.62f, 0.3f);
    static readonly Color Red = new Color(0.75f, 0.2f, 0.2f);
    static readonly Color Blue = new Color(0.2f, 0.45f, 0.8f);
    static readonly Color Gold = new Color(0.95f, 0.75f, 0.15f);
    static readonly Color Grey = new Color(0.35f, 0.38f, 0.42f);

    static Sprite box;

    [MenuItem("Memon Patta/Build GamePlay Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var n in new[] { "GameUI", "Network", "EventSystem" })
        {
            var old = GameObject.Find(n);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }
        box = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var cam = Camera.main;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Felt; }

        var net = new GameObject("Network").AddComponent<MatchNetwork>();
        net.name = "Network";

        // event system for the new Input System
        var es = new GameObject("EventSystem", typeof(EventSystem));
        var moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (moduleType != null)
        {
            var module = es.AddComponent(moduleType);
            var assign = moduleType.GetMethod("AssignDefaultActions");
            if (assign != null) assign.Invoke(module, null);
        }
        else es.AddComponent<StandaloneInputModule>();

        var root = new GameObject("GameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        var ui = root.AddComponent<GameUI>();
        var sprites = root.AddComponent<CardSprites>();
        sprites.faces = LoadCardSprites();
        ui.sprites = sprites;

        var bg = Img(root.transform, "Background", Felt);
        Fill(bg.rectTransform);
        bg.raycastTarget = false;

        BuildNamePanel(root.transform, ui);
        BuildLobbyPanel(root.transform, ui);
        BuildRoomPanel(root.transform, ui);
        BuildGamePanel(root.transform, ui);
        BuildSummaryPanel(root.transform, ui);
        BuildToast(root.transform, ui);

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("GamePlay scene built.");
    }

    // ---- sprites ----

    static Sprite[] LoadCardSprites()
    {
        string[] suits = { "clubs", "diamonds", "hearts", "spades" };
        string[] ranks = { "ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king" };
        var result = new Sprite[52];
        for (int s = 0; s < 4; s++)
            for (int r = 0; r < 13; r++)
            {
                string path = CardsFolder + ranks[r] + "_of_" + suits[s] + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { Debug.LogWarning("Missing card image: " + path); continue; }
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                result[s * 13 + r] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        return result;
    }

    // ---- panels ----

    static void BuildNamePanel(Transform parent, GameUI ui)
    {
        var p = Panel(parent, "NamePanel");
        ui.panelName = p;
        Label(p.transform, "Title", "Get Licensed", 96, Gold, 0, 520, 960, 130);
        Label(p.transform, "Subtitle", "Memon Patta", 54, Color.white, 0, 410, 960, 80);
        ui.nameInput = Input(p.transform, "NameInput", "Enter your name", 0, 150, 760, 120, 16);
        ui.connectBtn = Btn(p.transform, "ConnectBtn", "Play Online", Green, 0, -20, 760, 130, 48);
        ui.nameStatusText = Label(p.transform, "Status", "", 34, new Color(1f, 0.9f, 0.6f), 0, -190, 900, 120);
    }

    static void BuildLobbyPanel(Transform parent, GameUI ui)
    {
        var p = Panel(parent, "LobbyPanel");
        ui.panelLobby = p;
        ui.lobbyHelloText = Label(p.transform, "Hello", "Hello!", 60, Color.white, 0, 560, 960, 100);
        ui.createBtn = Btn(p.transform, "CreateBtn", "Create Private Room", Green, 0, 340, 760, 140, 46);
        ui.quickBtn = Btn(p.transform, "QuickBtn", "Quick Match", Blue, 0, 170, 760, 140, 46);
        Label(p.transform, "HaveCode", "Have a room code?", 38, Color.white, 0, 20, 760, 70);
        ui.codeInput = Input(p.transform, "CodeInput", "CODE", -150, -90, 440, 120, 6);
        ui.codeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        ui.codeInput.onValidateInput += (t, i, c) => char.ToUpperInvariant(c);
        ui.joinBtn = Btn(p.transform, "JoinBtn", "Join", Gold, 250, -90, 260, 120, 46);
        ui.backToNameBtn = Btn(p.transform, "BackBtn", "Change Name", Grey, 0, -380, 560, 110, 40);
    }

    static void BuildRoomPanel(Transform parent, GameUI ui)
    {
        var p = Panel(parent, "RoomPanel");
        ui.panelRoom = p;
        Label(p.transform, "Title", "Room Code", 50, Color.white, 0, 640, 900, 80);
        ui.roomCodeText = Label(p.transform, "Code", "-----", 130, Gold, 0, 520, 960, 170);
        ui.roomPlayersText = Label(p.transform, "Players", "", 42, Color.white, 0, 150, 840, 560);
        ui.roomHintText = Label(p.transform, "Hint", "", 34, new Color(1f, 0.9f, 0.6f), 0, -230, 880, 100);
        ui.startBtn = Btn(p.transform, "StartBtn", "Start Match", Green, 0, -380, 760, 140, 50);
        ui.leaveRoomBtn = Btn(p.transform, "LeaveBtn", "Leave Room", Red, 0, -550, 560, 120, 42);
    }

    static void BuildGamePanel(Transform parent, GameUI ui)
    {
        var p = Panel(parent, "GamePanel");
        ui.panelGame = p;
        var t = p.transform;

        // top bar
        var top = Area(t, "TopBar", 0, 0.94f, 1, 1);
        ui.exitBtn = Btn(top, "ExitBtn", "Exit", Red, 0, 0, 0, 0, 34);
        Corner(ui.exitBtn.GetComponent<RectTransform>(), 0, 0, 20, 14, 170, 84);
        ui.endMatchBtn = Btn(top, "EndMatchBtn", "End", Grey, 0, 0, 0, 0, 34);
        Corner(ui.endMatchBtn.GetComponent<RectTransform>(), 0, 0, 205, 14, 170, 84);
        ui.roundText = Label(top, "Round", "Round 1", 44, Color.white, 0, 0, 0, 0);
        Anchor(ui.roundText.rectTransform, 0.4f, 0, 0.75f, 1);
        ui.timerText = Label(top, "Timer", "", 48, Color.white, 0, 0, 0, 0);
        Anchor(ui.timerText.rectTransform, 0.75f, 0, 1, 1);
        ui.timerText.alignment = TextAlignmentOptions.MidlineRight;
        ui.timerText.rectTransform.offsetMax = new Vector2(-24, 0);

        // players bar
        var players = Area(t, "Players", 0, 0.86f, 1, 0.94f);
        var ph = players.gameObject.AddComponent<HorizontalLayoutGroup>();
        ph.padding = new RectOffset(10, 10, 4, 4); ph.spacing = 8;
        ph.childControlWidth = ph.childControlHeight = true; ph.childForceExpandWidth = true; ph.childForceExpandHeight = true;
        ui.playersContent = players;

        // melds
        var meldsHolder = Area(t, "MeldsScroll", 0.02f, 0.5f, 0.98f, 0.86f);
        ui.meldsContent = Scroll(meldsHolder, false, Dim, 8, TextAnchor.UpperLeft, 0);

        // draw pile + discard pile
        ui.drawBtn = Btn(t, "DrawBtn", "DRAW", Red, 0, 0, 0, 0, 34);
        var dr = ui.drawBtn.GetComponent<RectTransform>();
        Anchor(dr, 0.02f, 0.34f, 0.23f, 0.5f);
        var drawLabel = ui.drawBtn.GetComponentInChildren<TMP_Text>();
        Anchor(drawLabel.rectTransform, 0, 0.5f, 1, 1);
        ui.drawCountText = Label(ui.drawBtn.transform, "Count", "0", 64, Color.white, 0, 0, 0, 0);
        Anchor(ui.drawCountText.rectTransform, 0, 0, 1, 0.55f);

        var discardHolder = Area(t, "DiscardScroll", 0.25f, 0.34f, 0.98f, 0.5f);
        ui.discardContent = Scroll(discardHolder, true, Dim, -50, TextAnchor.MiddleLeft, 10);

        // log + take button
        ui.logText = Label(t, "Log", "", 28, Color.white, 0, 0, 0, 0);
        Anchor(ui.logText.rectTransform, 0.02f, 0.285f, 0.72f, 0.335f);
        ui.logText.alignment = TextAlignmentOptions.MidlineLeft;
        ui.logText.enableAutoSizing = true; ui.logText.fontSizeMin = 18; ui.logText.fontSizeMax = 28;
        ui.takeBtn = Btn(t, "TakeBtn", "Take", Gold, 0, 0, 0, 0, 34);
        Anchor(ui.takeBtn.GetComponent<RectTransform>(), 0.74f, 0.29f, 0.98f, 0.335f);
        ui.takeBtnText = ui.takeBtn.GetComponentInChildren<TMP_Text>();

        // hand
        var handHolder = Area(t, "HandScroll", 0.02f, 0.13f, 0.98f, 0.285f);
        ui.handContent = Scroll(handHolder, true, Dim, -70, TextAnchor.LowerLeft, 12);

        // actions
        var actions = Area(t, "Actions", 0.02f, 0.02f, 0.98f, 0.12f);
        var ah = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        ah.spacing = 10; ah.childControlWidth = ah.childControlHeight = true; ah.childForceExpandWidth = true; ah.childForceExpandHeight = true;
        ui.sortBtn = Btn(actions, "SortBtn", "Sort", Grey, 0, 0, 0, 0, 30);
        ui.playBtn = Btn(actions, "PlayBtn", "Play Meld", Green, 0, 0, 0, 0, 28);
        ui.extendBtn = Btn(actions, "ExtendBtn", "Extend", Blue, 0, 0, 0, 0, 28);
        ui.discardBtn = Btn(actions, "DiscardBtn", "Discard", Red, 0, 0, 0, 0, 28);
        ui.finishBtn = Btn(actions, "FinishBtn", "Finish", Gold, 0, 0, 0, 0, 28);
    }

    static void BuildSummaryPanel(Transform parent, GameUI ui)
    {
        var p = Panel(parent, "SummaryPanel");
        ui.panelSummary = p;
        var dim = Img(p.transform, "Dim", new Color(0, 0, 0, 0.78f));
        Fill(dim.rectTransform);
        var card = Img(p.transform, "Card", new Color(0.08f, 0.2f, 0.14f, 1f));
        card.sprite = box; card.type = Image.Type.Sliced;
        Center(card.rectTransform, 0, 0, 940, 1300);

        ui.summaryTitle = Label(card.transform, "Title", "Round complete", 60, Gold, 0, 560, 880, 100);
        ui.summaryBody = Label(card.transform, "Body", "", 36, Color.white, 0, 90, 860, 800);
        ui.summaryBody.alignment = TextAlignmentOptions.Top;
        ui.nextRoundBtn = Btn(card.transform, "NextRoundBtn", "Next Round", Green, 0, -440, 700, 120, 44);
        ui.summaryEndBtn = Btn(card.transform, "EndBtn", "End Match", Red, 0, -580, 700, 110, 40);
        ui.summaryCloseBtn = Btn(card.transform, "CloseBtn", "Back to Room", Blue, 0, -500, 700, 120, 44);
        ui.summaryWaitText = Label(card.transform, "Wait", "Waiting for the host...", 36, new Color(1f, 0.9f, 0.6f), 0, -500, 800, 80);
    }

    static void BuildToast(Transform parent, GameUI ui)
    {
        var holder = Img(parent, "Toast", new Color(0.55f, 0.1f, 0.1f, 0.95f));
        holder.sprite = box; holder.type = Image.Type.Sliced; holder.raycastTarget = false;
        Anchor(holder.rectTransform, 0.06f, 0.78f, 0.94f, 0.85f);
        ui.toastText = Label(holder.transform, "Text", "", 32, Color.white, 0, 0, 0, 0);
        Fill(ui.toastText.rectTransform);
        ui.toastText.rectTransform.offsetMin = new Vector2(16, 4);
        ui.toastText.rectTransform.offsetMax = new Vector2(-16, -4);
        ui.toastText.enableAutoSizing = true; ui.toastText.fontSizeMin = 20; ui.toastText.fontSizeMax = 32;
    }

    // ---- UI factory helpers ----

    static GameObject Panel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Fill(go.GetComponent<RectTransform>());
        return go;
    }

    static RectTransform Area(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        Anchor(rt, x0, y0, x1, y1);
        return rt;
    }

    static Image Img(Transform parent, string name, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = c;
        return img;
    }

    static TMP_Text Label(Transform parent, string name, string text, float size, Color color, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        if (w > 0) Center(t.rectTransform, x, y, w, h);
        return t;
    }

    static Button Btn(Transform parent, string name, string label, Color color, float x, float y, float w, float h, float fontSize)
    {
        var img = Img(parent, name, color);
        img.sprite = box; img.type = Image.Type.Sliced;
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        b.colors = colors;
        if (w > 0) Center(img.rectTransform, x, y, w, h);
        var t = Label(img.transform, "Label", label, fontSize, Color.white, 0, 0, 0, 0);
        Fill(t.rectTransform);
        t.fontStyle = FontStyles.Bold;
        t.enableAutoSizing = true; t.fontSizeMax = fontSize; t.fontSizeMin = 14;
        t.rectTransform.offsetMin = new Vector2(6, 4); t.rectTransform.offsetMax = new Vector2(-6, -4);
        return b;
    }

    static TMP_InputField Input(Transform parent, string name, string placeholder, float x, float y, float w, float h, int limit)
    {
        var img = Img(parent, name, new Color(0.96f, 0.96f, 0.96f));
        img.sprite = box; img.type = Image.Type.Sliced;
        Center(img.rectTransform, x, y, w, h);
        var field = img.gameObject.AddComponent<TMP_InputField>();

        var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(img.transform, false);
        var ar = area.GetComponent<RectTransform>();
        Fill(ar);
        ar.offsetMin = new Vector2(20, 8); ar.offsetMax = new Vector2(-20, -8);

        var ph = Label(area.transform, "Placeholder", placeholder, 44, new Color(0.45f, 0.45f, 0.45f), 0, 0, 0, 0);
        Fill(ph.rectTransform); ph.fontStyle = FontStyles.Italic;
        var tx = Label(area.transform, "Text", "", 48, new Color(0.1f, 0.1f, 0.1f), 0, 0, 0, 0);
        Fill(tx.rectTransform);
        tx.raycastTarget = false;

        field.textViewport = ar;
        field.textComponent = tx;
        field.placeholder = ph;
        field.characterLimit = limit;
        return field;
    }

    // Returns the scroll content. Horizontal rows use the given spacing; vertical lists stack their rows.
    static RectTransform Scroll(RectTransform holder, bool horizontal, Color bg, float spacing, TextAnchor align, int pad)
    {
        var bgImg = holder.gameObject.AddComponent<Image>();
        bgImg.color = bg;
        var sr = holder.gameObject.AddComponent<ScrollRect>();

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(holder, false);
        var vr = viewport.GetComponent<RectTransform>();
        Fill(vr);

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(vr, false);
        var cr = content.GetComponent<RectTransform>();
        var fitter = content.AddComponent<ContentSizeFitter>();
        if (horizontal)
        {
            cr.anchorMin = new Vector2(0, 0); cr.anchorMax = new Vector2(0, 1); cr.pivot = new Vector2(0, 0.5f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;
            var g = content.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing; g.childAlignment = align; g.padding = new RectOffset(pad, pad + 60, 6, 6);
            g.childControlWidth = g.childControlHeight = true; g.childForceExpandWidth = g.childForceExpandHeight = false;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        else
        {
            cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(0.5f, 1);
            cr.offsetMin = cr.offsetMax = Vector2.zero;
            var g = content.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing; g.childAlignment = align; g.padding = new RectOffset(8, 8, 8, 8);
            g.childControlWidth = g.childControlHeight = true; g.childForceExpandWidth = true; g.childForceExpandHeight = false;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        sr.viewport = vr;
        sr.content = cr;
        sr.horizontal = horizontal;
        sr.vertical = !horizontal;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30;
        return cr;
    }

    static void Fill(RectTransform rt) { Anchor(rt, 0, 0, 1, 1); }

    static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // Anchor to the top-left corner with a pixel offset and size.
    static void Corner(RectTransform rt, float ax, float ay, float offX, float offY, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(offX, -offY);
        rt.sizeDelta = new Vector2(w, h);
    }
}
