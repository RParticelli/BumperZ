using System;
using UnityEngine;

public sealed class BumperMainMenu : MonoBehaviour
{
    enum Page { Home, Shop, Garage, Friends, Clan, Missions, Event, Customize, Ranking, Seasons }
    static readonly Color Navy = new(.008f, .027f, .05f, .95f);
    static readonly Color Cyan = new(0, .67f, 1, 1);
    static readonly Color Gold = new(1, .63f, .04f, 1);
    Texture2D background, pixel, logo, panelBlue, panelGold, playSkin, modeBlue, modePurple, modeGreen, modeGold, iconSkin, bottomSkin;
    Texture2D navHome, navCars, navCustomize, navRanking, navSeasons;
    GUIStyle text, button;
    Page page;
    int selectedMode;
    float uiScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        PlayerPrefs.SetInt("BumperZ.InMatch", 0);
        var go = new GameObject("BumperMainMenu");
        DontDestroyOnLoad(go); go.AddComponent<BumperMainMenu>();
    }

    void Awake()
    {
        background = Resources.Load<Texture2D>("UI/Menu/MainMenuBackground");
        logo = Resources.Load<Texture2D>("UI/Menu/BumperRivalsLogo");
        panelBlue = Resources.Load<Texture2D>("UI/Menu/PanelBlue");
        panelGold = Resources.Load<Texture2D>("UI/Menu/PanelGold");
        playSkin = Resources.Load<Texture2D>("UI/Menu/PlayButton");
        modeBlue = Resources.Load<Texture2D>("UI/Menu/ModeBlue");
        modePurple = Resources.Load<Texture2D>("UI/Menu/ModePurple");
        modeGreen = Resources.Load<Texture2D>("UI/Menu/ModeGreen");
        modeGold = Resources.Load<Texture2D>("UI/Menu/ModeGold");
        iconSkin = Resources.Load<Texture2D>("UI/Menu/IconButton");
        bottomSkin = Resources.Load<Texture2D>("UI/Menu/BottomNav");
        navHome = Resources.Load<Texture2D>("UI/Menu/NavHome");
        navCars = Resources.Load<Texture2D>("UI/Menu/NavCars");
        navCustomize = Resources.Load<Texture2D>("UI/Menu/NavCustomize");
        navRanking = Resources.Load<Texture2D>("UI/Menu/NavRanking");
        navSeasons = Resources.Load<Texture2D>("UI/Menu/NavSeasons");
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white); pixel.Apply();
        selectedMode = PlayerPrefs.GetInt("BumperZ.SelectedMode", 0);
    }

    void OnGUI()
    {
        if (PlayerPrefs.GetInt("BumperZ.InMatch", 0) != 0) return;
        float s = Mathf.Min(Screen.width / 960f, Screen.height / 540f); uiScale = s;
        float ox = (Screen.width - 960 * s) * .5f, oy = (Screen.height - 540 * s) * .5f;
        Rect R(float x, float y, float w, float h) => new(ox + x * s, oy + y * s, w * s, h * s);
        if (text == null) { text = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; button = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true }; }

        GUI.color = Color.white;
        if (background) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), background, ScaleMode.ScaleAndCrop);
        GUI.color = new Color(0, 0, .02f, .16f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel); GUI.color = Color.white;
        TopBar(R);
        if (page == Page.Home) Home(R); else SubPage(R);
        BottomBar(R);
    }

    void TopBar(Func<float,float,float,float,Rect> R)
    {
        var p = BumperProfile.Data;
        Box(R(9, 10, 296, 49), Navy, Cyan);
        Box(R(14, 11, 56, 56), new Color(.02f, .11f, .2f, 1), Cyan);
        Label(R(14, 11, 56, 56), "RX", 19, Color.white);
        Label(R(79, 11, 130, 25), p.playerName, 19, Color.white, TextAnchor.MiddleLeft);
        Label(R(79, 35, 62, 19), $"NÍVEL {p.level}", 10, Color.white, TextAnchor.MiddleLeft);
        Label(R(144, 34, 152, 20), $"♛ {BumperProfile.RankName(p.trophies)}", 10, Gold, TextAnchor.MiddleLeft);
        Label(R(201, 10, 92, 23), $"TROFÉUS {p.trophies:N0}", 10, Gold);

        Currency(R(460, 14, 148, 39), "MOEDAS", p.coins, Gold, () => page = Page.Shop);
        Currency(R(612, 14, 142, 39), "GEMAS", p.gems, Cyan, () => page = Page.Shop);
        IconButton(R(768, 14, 51, 42), "AMIGOS", () => page = Page.Friends);
        IconButton(R(827, 14, 51, 42), $"MSG {p.unreadMessages}", () => { p.unreadMessages = 0; BumperProfile.Save(); page = Page.Friends; });
        IconButton(R(886, 14, 51, 42), "AJUSTES", () => page = Page.Customize);
    }

    void Home(Func<float,float,float,float,Rect> R)
    {
        var p = BumperProfile.Data;
        if (logo) GUI.DrawTexture(R(14, 62, 282, 137), logo, ScaleMode.ScaleToFit, true);

        Skin(R(14, 201, 268, 78), panelGold);
        Label(R(25, 205, 155, 21), "PASSE DE BATALHA", 13, Gold, TextAnchor.MiddleLeft);
        Label(R(25, 225, 150, 18), BumperLiveContent.Data.seasonName, 11, Cyan, TextAnchor.MiddleLeft);
        Label(R(24, 245, 50, 25), (p.passPoints / 500 + 1).ToString(), 18, Gold);
        Label(R(72, 245, 84, 22), $"{p.passPoints % 500}/500", 13, Color.white);
        Progress(R(73, 265, 100, 6), (p.passPoints % 500) / 500f, Gold);
        Label(R(190, 217, 78, 48), "♛", 38, Gold);

        Skin(R(14, 286, 268, 119), panelBlue);
        Label(R(25, 289, 220, 20), "MISSÕES DIÁRIAS", 12, Cyan, TextAnchor.MiddleLeft);
        Mission(R, 315, "Faça 3 partidas", Mathf.Min(p.matches, 3), 3, 250);
        Mission(R, 344, "Use o impulso 5 vezes", Mathf.Min(p.matches * 2, 5), 5, 250);
        Mission(R, 373, "Vença 2 partidas", Mathf.Min(p.wins, 2), 2, 250);
        if (Hot(R(22, 389, 252, 13), "VER TODAS", new Color(.04f, .08f, .13f, 1), Color.white, 9)) page = Page.Missions;

        Skin(R(740, 72, 206, 111), panelGold);
        Label(R(752, 79, 183, 18), "EVENTO ESPECIAL", 11, Gold, TextAnchor.MiddleLeft);
        Label(R(752, 98, 183, 26), BumperLiveContent.Data.eventName, 18, Color.white, TextAnchor.MiddleLeft);
        Label(R(752, 124, 183, 19), "Termina em: " + BumperLiveContent.EventRemaining(), 11, Color.white, TextAnchor.MiddleLeft);
        if (Hot(R(748, 145, 190, 31), "VER EVENTO", new Color(.22f, .04f, .01f, .9f), Gold, 11)) page = Page.Event;

        Side(R, 740, 190, "LOJA", "Skins, rodas e muito mais!", Gold, Page.Shop);
        Side(R, 740, 238, "GARAGEM", "Personalize seu carrinho!", Gold, Page.Garage);
        Side(R, 740, 286, "AMIGOS", "Jogue com seus amigos!", Gold, Page.Friends);
        Side(R, 740, 334, "CLÃ", "Crie ou entre em um clã!", Gold, Page.Clan);

        Mode(R, 18, 168, "PARTIDA RÁPIDA", "Entre e jogue!", Cyan, 0);
        Mode(R, 190, 168, "RANQUEADO", "Suba no ranking", new Color(.75f, .35f, 1, 1), 1);
        Mode(R, 362, 144, "EQUIPE", "Jogue em equipe", new Color(.1f, 1, .55f, 1), 2);
        Mode(R, 510, 140, "EVENTOS", "Modos especiais", Gold, 3);
        if (SkinnedHot(R(660, 407, 286, 75), "JOGAR  ▶", playSkin, Gold, 34)) StartGame();
    }

    void BottomBar(Func<float,float,float,float,Rect> R)
    {
        Skin(R(10, 489, 936, 42), bottomSkin);
        Nav(R(10, 489, 170, 42), "INÍCIO", Page.Home, navHome);
        Nav(R(180, 489, 170, 42), "CARRINHOS", Page.Garage, navCars);
        Nav(R(350, 489, 190, 42), "PERSONALIZAÇÃO", Page.Customize, navCustomize);
        Nav(R(540, 489, 170, 42), "RANKING", Page.Ranking, navRanking);
        Nav(R(710, 489, 236, 42), "TEMPORADAS", Page.Seasons, navSeasons);
    }

    void SubPage(Func<float,float,float,float,Rect> R)
    {
        Box(R(105, 78, 750, 380), new Color(.005f, .025f, .048f, .98f), Accent(page));
        Label(R(130, 92, 540, 42), Title(page), 28, Accent(page), TextAnchor.MiddleLeft);
        if (Hot(R(765, 91, 62, 32), "VOLTAR", Navy, Color.white, 10)) page = Page.Home;
        string body = page switch
        {
            Page.Shop => "SALDO ATUAL\n\nMoedas e gemas estão conectadas ao seu perfil.\nAs ofertas serão carregadas pelo catálogo do servidor.",
            Page.Garage => "SEUS CARRINHOS\n\nO carro equipado será salvo no perfil e usado nas partidas.",
            Page.Friends => "AMIGOS E CONVITES\n\nLista preparada para receber presença, convites e salas online.",
            Page.Clan => "CLÃ\n\nCrie, pesquise ou entre em um clã quando o serviço online estiver conectado.",
            Page.Missions => $"PROGRESSO ATUAL\n\nPartidas: {BumperProfile.Data.matches}\nVitórias: {BumperProfile.Data.wins}\nAs missões usam resultados reais das partidas.",
            Page.Event => BumperLiveContent.Data.eventName + "\n\nTermina em " + BumperLiveContent.EventRemaining() + ".\nRegras e recompensas vêm do catálogo atualizável.",
            Page.Customize => "PERSONALIZAÇÃO E AJUSTES\n\nControles, áudio, gráficos e aparência do jogador.",
            Page.Ranking => $"SEU RANK\n\n{BumperProfile.RankName(BumperProfile.Data.trophies)}  •  {BumperProfile.Data.trophies:N0} troféus",
            _ => $"TEMPORADA 3\n\nPasse: {BumperProfile.Data.passPoints % 500}/500\nNível do passe: {BumperProfile.Data.passPoints / 500 + 1}"
        };
        Label(R(145, 150, 670, 180), body, 17, Color.white, TextAnchor.UpperLeft);
        if (page == Page.Shop) Label(R(145, 350, 500, 55), "Compras serão habilitadas somente com preços e recibos reais da loja.", 13, Gold, TextAnchor.MiddleLeft);
        if (page == Page.Garage && Hot(R(145, 350, 210, 55), "EQUIPAR CARRO ATUAL", Navy, Cyan, 13)) PlayerPrefs.SetString("BumperZ.EquippedCar", "Default");
    }

    void Mission(Func<float,float,float,float,Rect> R, float y, string name, int current, int target, int reward)
    {
        Label(R(31, y, 145, 17), name, 10, Color.white, TextAnchor.MiddleLeft);
        Label(R(177, y, 28, 17), $"{current}/{target}", 9, Color.white);
        Label(R(218, y, 54, 17), $"◆ {reward}", 9, Cyan);
        Progress(R(31, y + 19, 146, 4), current / (float)target, Cyan);
    }
    void Progress(Rect r, float value, Color color) { Fill(r, new Color(.08f, .13f, .19f, 1)); Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value), r.height), color); }
    void Side(Func<float,float,float,float,Rect> R, float x, float y, string name, string sub, Color accent, Page target)
    {
        if (SkinnedHot(R(x, y, 206, 42), name + "\n" + sub, panelBlue, accent, 11)) page = target;
    }
    void Mode(Func<float,float,float,float,Rect> R, float x, float width, string name, string sub, Color accent, int mode)
    {
        Color edge = selectedMode == mode ? Color.white : accent;
        Texture2D skin = mode == 0 ? modeBlue : mode == 1 ? modePurple : mode == 2 ? modeGreen : modeGold;
        if (SkinnedHot(R(x, 408, width, 73), name + "\n" + sub, skin, edge, 12)) { selectedMode = mode; PlayerPrefs.SetInt("BumperZ.SelectedMode", mode); }
    }
    void Nav(Rect r, string name, Page target, Texture2D icon)
    {
        bool active = page == target;
        if (active)
        {
            Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), new Color(0, .42f, .75f, .34f));
            Fill(new Rect(r.x + 8, r.y + r.height - 2, r.width - 16, 2), Cyan);
        }
        if (icon) GUI.DrawTexture(new Rect(r.x + 22 * uiScale, r.y + 6 * uiScale, 30 * uiScale, 30 * uiScale), icon, ScaleMode.ScaleToFit, true);
        Label(new Rect(r.x + 56 * uiScale, r.y, r.width - 62 * uiScale, r.height), name, 10, active ? Color.white : new Color(.72f, .75f, .82f, 1), TextAnchor.MiddleLeft);
        Fill(new Rect(r.x + r.width - 1, r.y + 7 * uiScale, 1, r.height - 14 * uiScale), new Color(.18f, .28f, .38f, .7f));
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) page = target;
    }
    void Currency(Rect r, string name, int value, Color accent, Action action) { if (SkinnedHot(r, $"●  {value:N0}    +", accent == Gold ? panelGold : panelBlue, accent, 16)) action(); }
    void IconButton(Rect r, string name, Action action) { if (SkinnedHot(r, name, iconSkin, Color.white, 9)) action(); }

    bool SkinnedHot(Rect r, string value, Texture2D skin, Color textColor, int size)
    {
        Skin(r, skin); bool hit = GUI.Button(r, GUIContent.none, GUIStyle.none); Label(r, value, size, textColor); return hit;
    }
    void Skin(Rect r, Texture2D skin) { if (skin) GUI.DrawTexture(r, skin, ScaleMode.StretchToFill, true); else Box(r, Navy, Cyan); }

    bool Hot(Rect r, string value, Color fill, Color edge, int size)
    {
        Box(r, fill, edge); bool hit = GUI.Button(r, GUIContent.none, GUIStyle.none); Label(r, value, size, edge); return hit;
    }
    void Box(Rect r, Color fill, Color edge) { Fill(r, edge); Fill(new Rect(r.x + 1.5f, r.y + 1.5f, r.width - 3, r.height - 3), fill); }
    void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, pixel); GUI.color = Color.white; }
    void Label(Rect r, string value, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter) { text.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * uiScale)); text.normal.textColor = color; text.alignment = align; text.wordWrap = true; GUI.Label(r, value, text); }
    static string Title(Page p) => p switch { Page.Shop => "LOJA", Page.Garage => "GARAGEM", Page.Friends => "AMIGOS", Page.Clan => "CLÃ", Page.Missions => "MISSÕES DIÁRIAS", Page.Event => "EVENTO ESPECIAL", Page.Customize => "PERSONALIZAÇÃO", Page.Ranking => "RANKING", _ => "TEMPORADAS" };
    static Color Accent(Page p) => p == Page.Event || p == Page.Shop || p == Page.Seasons ? Gold : p == Page.Friends || p == Page.Clan ? new Color(.1f, 1, .55f, 1) : Cyan;
    void StartGame() { PlayerPrefs.SetInt("BumperZ.InMatch", 1); if (!FindAnyObjectByType<BumperGame>()) new GameObject("BumperGame").AddComponent<BumperGame>(); }
}
