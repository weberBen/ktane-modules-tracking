using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Threading;
using System;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Globalization;


public class Logger {

    public string file;
    public string contextName;

    public static string defaultFile = "/home/benjamin/log_test.log";

    public Logger(bool toFile=false) {
        contextName = null;
        file = (toFile) ? Logger.defaultFile : null;
    }

    public Logger(string contextName, bool toFile=false) {
        this.contextName = contextName;
        this.file = (toFile) ? Logger.defaultFile : null;
    }

    public Logger(string contextName, string file) {
        this.contextName = contextName;
        this.file = file;
    }

    public void Log(string msg) {

        string logLine = String.Format("[{0}] : " + ((contextName == null) ? "{1}" : "<{1}>") + " {2}",
            DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), ((contextName == null) ? "" : contextName), msg);

        if (file == null) {
            Debug.Log(logLine);
        } else {
            File.AppendAllText(file, logLine + "\n");
        }
    }

    public void Clear() {
        if (file == null) {
            return ;
        }

        File.WriteAllText(file, "");
    }
}

public class BombData {

    public Component bomb;
    public Dictionary<string, Component> modulesMap;
    public List<Component> modules;
    public List<Component> modulesFront;
    public List<Component> modulesRear;
    public Component floatingHoldable;
    public GameObject visual;
    public GameObject visualTransform;
    public GameObject widgetAreas;
    public Dictionary<int, PanelPosition> modulesPanelPosition; 
    public Dictionary<int, Component> modulesIdMap; 
    public Dictionary<string, Component> modulesPanelPositionMap;
    public Dictionary<string, GameObject> modulesTable;
    public Texture2D foamTexture;

    public static List<PanelPosition> gridPositions = new List<PanelPosition> {
            new BombData.PanelPosition(0, 0, 0),
            new BombData.PanelPosition(0, 0, 1),
            new BombData.PanelPosition(0, 0, 2),
            new BombData.PanelPosition(0, 1, 0),
            new BombData.PanelPosition(0, 1, 1),
            new BombData.PanelPosition(0, 1, 2),

            new BombData.PanelPosition(1, 0, 0),
            new BombData.PanelPosition(1, 0, 1),
            new BombData.PanelPosition(1, 0, 2),
            new BombData.PanelPosition(1, 1, 0),
            new BombData.PanelPosition(1, 1, 1),
            new BombData.PanelPosition(1, 1, 2),
    };

    public BombData() {

    }

    public class PanelPosition {
        public int face;
        public int row;
        public int col;

        public PanelPosition(int face,  int row, int col) {
            this.face = face;
            this.row = row;
            this.col = col;
        }

        public string ToIndex() {
            return String.Format("{0}|{1}|{2}", face, row, col);
        }

        public static string ToIndex(int face, int row, int col) {
            return String.Format("{0}|{1}|{2}", face, row, col);
        }

        public static string ToIndex(int face, float row, float col) {
            return String.Format("{0}|{1}|{2}", face, row, col);
        }
    }

}

public class WebRequestData {
    public bool reset;
    public int test;
    public string value;
    public string rawResponse;
    public List<HighlightModule> highlightModules;
    public string resourceId;
    public TransformStruct rotation;
    public TransformStruct position;
    public List<int> moduleIds;
    public List<BombData.PanelPosition> moduleGridPositions;

    public WebRequestData() {

    }

    public WebRequestData(string rawResponse) {
        this.rawResponse = rawResponse;
    }

    public class ColorStruct {
        public float r;
        public float g;
        public float b;
        public float a;

        public ColorStruct() {
            a = 1;
        }
    }
    public class HighlightModule {
        public int face;
        public int row;
        public int col;
        public ColorStruct color;

        public HighlightModule() {}
    }

    public class TransformStruct {
        public float x;
        public float y;
        public float z;
        public float w;
        public string relativeTo;
        public string format;
        public string transformation;

        public TransformStruct() {}
    }
}


public class SceneData {

    public Component facilityRoom;
    public Assembly gameAssembly;
    public Component sceneManager;
    public bool isGameSceneOn;
    public bool missionLoaded;

    public SceneData(Assembly asm) {
        gameAssembly = asm;
        isGameSceneOn = true;
        missionLoaded = false;
    }
}


public class WebHook {
    public string addr;
    private Logger logger;
    private string uuid;
    private string serializedDefaultPlayload;
    private JObject defaultPlayload;

    public WebHook(string address) {
        logger = new Logger("WebHook", true);
        addr = address;
        uuid = System.Guid.NewGuid().ToString();

        defaultPlayload = JObject.FromObject(new {
            uuid = uuid,
        });
        serializedDefaultPlayload = defaultPlayload.ToString(0);
    }

    private string SerializedRequestData(object obj) {

        if (obj==null) {
            return serializedDefaultPlayload;
        }

        var result = new JObject();
        result.Merge(JObject.FromObject(new { header = defaultPlayload}));
        result.Merge(JObject.FromObject(new { data = ((obj==null) ? new {} : obj) }));

        return result.ToString(0);
    }

    private string GetUri(string url) {
        string uri = addr;
        if (url.Length>0) {
            if (url[0]=='/') {

                if (url.Length==1) {
                    url = "";
                } else {
                    url = url.Substring(1);
                }
            }

            if ((url.Length>0) && (url[url.Length-1]=='/')) {
                if (url.Length==1) {
                    url = "";
                } else {
                    url = url.Substring(0, url.Length-2);
                }
            }
        }

        if (url.Length>0) {
            uri += url;
        }

        return uri;
    }

    public HttpWebResponse SendPost(string url = "/", object data = null) {
        logger.Log("addr="+addr);
        try {
            if (addr==null)
                return null;

            string uri = GetUri(url);

            logger.Log("SendPost at url="+ uri);

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.ContentType = "application/json";

            string json = SerializedRequestData(data);
            logger.Log("SEndPost with data=" + json);
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(json);
            }

            logger.Log("start request");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            logger.Log("response retrived");
            
            return response;
        } catch(Exception e) {
            logger.Log(String.Format("Exception<{0}> : {1}", e.GetType(), e.Message));
            logger.Log(String.Format("Exception stack trace : {0}", e.StackTrace));

            return null;
        }
    }

    public string GetResponse(HttpWebResponse response) {
        string result = "";
        using (var streamReader = new StreamReader(response.GetResponseStream()))
        {
            result = streamReader.ReadToEnd();
        }

        return result;
    }

    private string HandleResponse(HttpWebResponse response, HttpStatusCode statusCode=HttpStatusCode.OK) {
        try {

            if (response==null) {
                return null;
            }

            if (response.StatusCode != statusCode) {
                return null;
            }

            return GetResponse(response);

        } catch(Exception e) {
            logger.Log(String.Format("Exception<{0}> : {1}", e.GetType(), e.Message));
            logger.Log(String.Format("Exception stack trace : {0}", e.StackTrace));

            return null;
        }
    }

    public bool GameTrackingInitiated(object data) {
        HttpWebResponse response = SendPost("/gameTrackingInitiated", data);
        var resp = HandleResponse(response);

        return (resp==null) ? false : true;
    }

    public bool ModulesHighlighted(object data) {
        HttpWebResponse response = SendPost("/modulesHighlighted", new { highlightedModules = data });
        var resp = HandleResponse(response);

        return (resp==null) ? false : true;
    }

    public bool ModulesHidden(object data) {
        HttpWebResponse response = SendPost("/modulesHidden", new { hiddenModules = data });
        var resp = HandleResponse(response);

        return (resp==null) ? false : true;
    }

    public void UploadScreenshot(byte[] data, string resourceId) {
        using (var wc = new WebClient())
        {
            string uri = GetUri(String.Format("/screenshot/{0}/{1}", uuid, resourceId));
            wc.UploadData(uri, "POST", data);
        }
    }


    public bool Register(string addr) {

        var data = new {
            addr = addr,
        };

        HttpWebResponse response = SendPost("/register", data);
        string resp = HandleResponse(response);

        return (resp==null) ? false : true;

    }
}

public class MeshWrapper {
    public MeshFilter meshFilter;
    public Renderer renderer;

    public MeshWrapper(MeshFilter meshFilter=null, Renderer renderer=null) {
        meshFilter = meshFilter;
        renderer = renderer;
    }

    public bool IsEmpty() {
        return ((meshFilter==null) || meshFilter.sharedMesh.vertices.Length==0) && (renderer==null);
    }
}

public class CoroutineManager {
    private Dictionary<string, Queue<string>> queue;
    private Dictionary<string, IEnumerator> IEnumeratorActions;
    private Dictionary<string, Action> actions;
    private Func<IEnumerator, object> StartCoroutine;
    private Queue<string> toStart;

    public CoroutineManager(Func<IEnumerator, object> StartCoroutineMethod) {
        queue = new Dictionary<string, Queue<string>>();
        IEnumeratorActions = new Dictionary<string, IEnumerator>();
        actions = new Dictionary<string, Action>();
        toStart = new Queue<string>();
        StartCoroutine = StartCoroutineMethod;
    }

    public string GetNewId() {
        return System.Guid.NewGuid().ToString();
    }


    public IEnumerator ConvertToIEnumerator(Action action) {
        action();
        yield return null;
    }

    public string Register(IEnumerator iEnumerator, string id=null) {
        if (id==null) {
            id = GetNewId();
        }

        IEnumeratorActions[id] = iEnumerator;

        return id;
    }

    public string Register(Action action, string id=null) {
        if (id==null) {
            id = GetNewId();
        }

        actions[id] = action;

        return id;
    }

    public void Link(string id, List<string> nextIds) {

        if (!actions.ContainsKey(id) && ! IEnumeratorActions.ContainsKey(id)) { // previous action ended
            return ;
        }

        if (!queue.ContainsKey(id)) {
            queue[id] = new Queue<string>();
        }

        foreach(string nextId in nextIds) {
            queue[id].Enqueue(nextId);
        }
    }

    public void Link(string[] ids) {

        for(int i=0; i<ids.Length-1; i++) {
            string id = ids[i];
            string nextId = ids[i+1];

            Link(id, nextId);
        }
    }

    public void Link(List<string> ids) {

        for(int i=0; i<ids.Count-1; i++) {
            string id = ids[i];
            string nextId = ids[i+1];

            Link(id, nextId);
        }
    }

    public void Link(string id, string nextId) {
        Link(id, new List<string>() {nextId});
    }

    private IEnumerator CoroutineWrapper(string id) {
        IEnumerator iEnumerator = IEnumeratorActions[id];
        
        while (iEnumerator.MoveNext())
        {
            yield return iEnumerator.Current;
        }


        if (queue.ContainsKey(id)) {
            while (queue[id].Count>0) {
                string nextId = queue[id].Dequeue();
                Start(nextId);
            }

            queue.Remove(id);
        }

        IEnumeratorActions.Remove(id);
    }

    private void StartAction(string id) {
        Action action = actions[id];
        
        action();

        if (queue.ContainsKey(id)) {
            while (queue[id].Count>0) {
                string nextId = queue[id].Dequeue();
                Start(nextId);
            }

            queue.Remove(id);
        }

        actions.Remove(id);
    }

    public void Start(string id) {
        if (!IEnumeratorActions.ContainsKey(id) && !actions.ContainsKey(id)) {
            throw new Exception(String.Format("Coroutine/Action {0} not registered", id));
        }

        if (IEnumeratorActions.ContainsKey(id)) {
            StartCoroutine(CoroutineWrapper(id));
        
        } else if(actions.ContainsKey(id)) {
            StartAction(id);
        }
    }


    public void Start(IEnumerator action) {
        Start(Register(action));
    }

    public void Start(Action action, bool startAsIEnumerator=false) {
        if (startAsIEnumerator) {
            Start(ConvertToIEnumerator(action));
        } else {
            Start(Register(action));
        }
    }

    public void QueueStart(string id) {
        toStart.Enqueue(id);
    }

    public void Start() {
        while (toStart.Count>0) {
            string id = toStart.Dequeue();
            Start(id);
        }
    }
}

public class GameTrackingException : Exception
{
    public int errorCode;

    public GameTrackingException() {}

    public GameTrackingException(string message): base(message) {}

    public GameTrackingException(int errorCode): base(null) {
        this.errorCode = errorCode;
    }

    public GameTrackingException(string message, int errorCode): base(message) {
        this.errorCode = errorCode;
    }

    public GameTrackingException(string message, Exception inner): base(message, inner) {}
}

public class TrackingModeSettings {
    [SerializeField]
    public string port;
    [SerializeField]
    public string host;
    [SerializeField]
    public string method;
    [SerializeField]
    public string webHookAddr;
    [SerializeField]
    public string stringFloatFormat;
    [SerializeField]
    public string stringIntFormat;
    [SerializeField]
    public string stringFloatCultureInfo;
    [SerializeField]
    public string stringIntCultureInfo;
    
    [NonSerialized]
    public int[] portRange;

    [NonSerialized]
    public static int[] defaultPortRange = new int[] {8081, 8085};
    [NonSerialized]
    public static string defaultAddr = "http://127.0.0.1:8085";
    [NonSerialized]
    private static string modSettingFilename = "TrackingSettings.json";

    [NonSerialized]
    private Logger logger;

    public TrackingModeSettings() {
        webHookAddr = null;
        host = "127.0.0.1";
        port = PortRangeToString(TrackingModeSettings.defaultPortRange);
        method = "http";
        logger = new Logger("TrackingModeSettings", true);
        stringIntFormat = "R";
        stringFloatFormat = "R";
        stringFloatCultureInfo = "invariant";
        stringIntCultureInfo = "invariant";
    }

    private void ParsePort(string port, ref int startRange, ref int endRange) {
        string[] items = port.Split(':');
        
        if (items.Length==0) {
            startRange = TrackingModeSettings.defaultPortRange[0];
            endRange = TrackingModeSettings.defaultPortRange[1];
        
        } else if (items.Length==1) {
            try {
                startRange = Int32.Parse(items[0]);
            } catch(FormatException) {
                startRange = TrackingModeSettings.defaultPortRange[0];
            }

            endRange = startRange;

        } else {
            try {
                startRange = Int32.Parse(items[0]);
                endRange = Int32.Parse(items[1]);
            } catch(FormatException) {
                startRange = TrackingModeSettings.defaultPortRange[0];
                endRange = TrackingModeSettings.defaultPortRange[1];
            }
        }

        if (startRange > endRange) {
            int tmp = startRange;
            startRange = endRange;
            endRange = tmp;
        }
    }

    private string PortRangeToString(int[] range) {
        return String.Format("{0}:{1}", range[0], range[1]);
    }

    public static TrackingModeSettings Load() {

        try
        {
            var settingsPath = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), TrackingModeSettings.modSettingFilename);

            TrackingModeSettings settings;
            if (!File.Exists(settingsPath)) {
                settings = new TrackingModeSettings();
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            } else {
                settings = JsonConvert.DeserializeObject<TrackingModeSettings>(File.ReadAllText(settingsPath));
            }

            settings.Parse();

            return settings;
        }
        catch (Exception e)
        {
            Debug.LogFormat(@"[Tracking Mode] Error : {1} ({2})\n{3}", e.Message, e.GetType().FullName, e.StackTrace);
            return null;
        }
    }

    private void Parse() {

        // port range
        int startRange = -1;
        int endRange = -1;
        

        ParsePort(port, ref startRange, ref endRange);
        portRange = new int[] {startRange, endRange};

        // webhook addr
        if (webHookAddr!=null && webHookAddr.Length==0) {
            webHookAddr = null;
        }
    }
}

public class ExampleWebService : MonoBehaviour
{
    KMBombInfo bombInfo;

    Thread workerThread;
    Worker workerObject;
    Queue<Action> actions;

    CoroutineManager coroutineManager;

    TrackingModeSettings settings;

    private Logger logger;

    BombData bombData;
    SceneData sceneData;

    public int attempt;

    void Awake()
    {
        attempt = 0;
        logger = new Logger("ExampleWebService", true);
        logger.Clear();

        try {
            settings = TrackingModeSettings.Load();

            actions = new Queue<Action>();
            coroutineManager = new CoroutineManager(StartCoroutine);

            
            sceneData = new SceneData(Assembly.Load("Assembly-CSharp"));
            bombInfo = GetComponent<KMBombInfo>();
            bombData = new BombData();
        
            workerObject = new Worker(this);
            workerThread = new Thread(workerObject.DoWork);
            workerThread.Start(this);
        }catch(Exception e) {
            logger.Log(String.Format("Exception<{0}> : {1}", e.GetType(), e.Message));
            logger.Log(String.Format("Exception stack trace : {0}", e.StackTrace));
        }
    }


    void Update()
    {
        if(actions.Count > 0)
        {
            Action action = actions.Dequeue();
            action();
        }
    }

    public string FormatNumber(int val) {
        string format = settings.stringIntFormat;
        string cultureInfo = settings.stringIntCultureInfo;
        
        return FormatNumber(val, format, cultureInfo);
    }

    public string FormatNumber(int val, string format, string cultureInfo) {
        if(format==null)
            return val.ToString();
        
        if(cultureInfo==null)
            return val.ToString(format);
        
        if(cultureInfo.Equals("invariant"))
            return val.ToString(format, CultureInfo.InvariantCulture);
        
        return val.ToString(format, CultureInfo.CreateSpecificCulture(cultureInfo));
    }

    public string FormatNumber(float val) {
        string format = settings.stringFloatFormat;
        string cultureInfo = settings.stringFloatCultureInfo;
        
        return FormatNumber(val, format, cultureInfo);
    }

    public string FormatNumber(float val, string format, string cultureInfo) {
        if(format==null)
            return val.ToString();
        
        if(cultureInfo==null)
            return val.ToString(format);
        
        if(cultureInfo.Equals("invariant"))
            return val.ToString(format, CultureInfo.InvariantCulture);
        
        return val.ToString(format, CultureInfo.CreateSpecificCulture(cultureInfo));
    }

    IEnumerator TakeScreenShot(Action<byte[]> callback = null, int x=0, int y=0, int width=-1, int height=-1)
    {
        yield return new WaitForEndOfFrame();

        width = (width<0) ? Screen.width : width;
        height = (height<0) ? Screen.height : height;
        x = (x<0) ? 0 : x;
        y = (y<0) ? 0 : y;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(x, y, width, height), 0, 0);
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);

        if(callback != null) {
            callback(bytes);
        }
    }

   
    void OnDestroy()
    {
        workerThread.Abort();
        workerObject.Stop();
    }

    public Rect GetScreenSpaceBounds(GameObject go, Renderer renderer, Camera camera) {
        Vector3 cen = renderer.bounds.center;
        Vector3 ext = renderer.bounds.extents;

        Vector2[] extentPoints = new Vector2[8]
        {
            WorldToGUIPoint(camera, new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z-ext.z)),
            WorldToGUIPoint(camera, new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z-ext.z)),
            WorldToGUIPoint(camera, new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z+ext.z)),
            WorldToGUIPoint(camera, new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z+ext.z)),
            WorldToGUIPoint(camera, new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z-ext.z)),
            WorldToGUIPoint(camera, new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z-ext.z)),
            WorldToGUIPoint(camera, new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z+ext.z)),
            WorldToGUIPoint(camera, new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z+ext.z))
        };

        Vector2 min = extentPoints[0];
        Vector2 max = extentPoints[0];

        foreach (Vector2 v in extentPoints)
        {
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);
        }

        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    protected Vector2 WorldToGUIPoint(Camera camera, Vector3 world) {
        Vector2 screenPoint = camera.WorldToScreenPoint(world);
        screenPoint.y = (float) Screen.height - screenPoint.y;
        return screenPoint;
    }

    protected List<Vector3> GetScreenSpaceVertices(GameObject go, MeshFilter meshFilter, Camera camera) {
        Vector3[] vertices = meshFilter.sharedMesh.vertices; 
        if (vertices.Length==0) {
            return new List<Vector3>();
        }

        List<Vector3> output = new List<Vector3>(vertices.Length);

        for (int i = 0; i < vertices.Length; i++) 
        { 
            // World space 
            vertices[i] = go.transform.TransformPoint(vertices[i]); 
            // GUI space 
            vertices[i] = WorldToGUIPoint(camera, vertices[i]);

            output.Add(vertices[i]);
        }
        
        return output;
    }

    protected Rect GetBoundingBox(GameObject go, Renderer renderer, MeshFilter meshFilter, Camera camera, bool fallbackBounds=true) {
        string type = null;
        return GetBoundingBox(go, renderer, meshFilter, camera, fallbackBounds);
    }

    protected Rect GetBoundingBox(GameObject go, Renderer renderer, MeshFilter meshFilter, Camera camera, ref string type, bool fallbackBounds=true) {        
        type = null;

        if (meshFilter==null) {
            if (fallbackBounds) {
                type = "meshBound";
                return GetScreenSpaceBounds(go, renderer, camera);
            }
        }

        type = "verticesBound";

        List<Vector3> vertices = GetScreenSpaceVertices(go, meshFilter, camera);
        if (vertices.Count==0) {

            if (fallbackBounds) {
                return GetScreenSpaceBounds(go, renderer, camera);
            }

            return new Rect(-2, -2, -2, -2);
        }
        
        Vector3 min = vertices[0]; 
        Vector3 max = vertices[0]; 
        for (int i = 1; i < vertices.Count; i++) 
        { 
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]); 
        } 
        
        // Construct a rect of the min and max positions
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    protected MeshWrapper GetBombComponentMesh(GameObject go) {
    MeshWrapper output = new MeshWrapper();

    if (go.name.ToLower().Contains("emptycomponent")) {
        output.meshFilter = go.GetComponentInChildren<MeshFilter>();
        output.renderer = go.GetComponentInChildren<Renderer>();

        return output;
    
    } else if (go.name.ToLower().Contains("timercomponent")) {
        GameObject got = GameObject.Find("Component_Timer");

        output.meshFilter = got.GetComponent<MeshFilter>();
        output.renderer = got.GetComponent<Renderer>();
        
        return output;
    }

    MeshFilter meshFilter = go.GetComponent<MeshFilter>();
    Renderer renderer = go.GetComponent<Renderer>();

    if (meshFilter == null || renderer == null) {

        foreach (Transform child in go.transform)
        {
            if (child.name.ToLower().Contains("highlight")) {
                meshFilter = child.gameObject.GetComponent<MeshFilter>();
                renderer = child.gameObject.GetComponent<Renderer>();
                break;
            }
        }
    }

    output.meshFilter = meshFilter;
    output.renderer = renderer;

    return output;
    }

    protected MeshWrapper GetBombMesh(GameObject go) {
        MeshWrapper output = new MeshWrapper();

        MeshFilter meshFilter = null;
        Renderer renderer = null;

        foreach (Transform child in bombData.visual.transform)
        {
            if (child.name.ToLower().Contains("highlight")) {
                meshFilter = child.gameObject.GetComponent<MeshFilter>();
                renderer = child.gameObject.GetComponent<Renderer>();
                break;
            }
        }

        output.meshFilter = meshFilter;
        output.renderer = renderer;

        return output;
    }

    protected Rect GetBombComponentBoundingBox(GameObject go) {
        string type = null;
        return GetBombComponentBoundingBox(go, ref type);
                
    }

    protected Rect GetBombComponentBoundingBox(GameObject go, ref string type) {
        MeshWrapper mesh = GetBombComponentMesh(go);
        if (mesh.IsEmpty())
            return new Rect(-1, -1, -1, -1);

        return GetBoundingBox(go, mesh.renderer, mesh.meshFilter, Camera.main, ref type);
                
    }

    protected Rect GetBombBoundingBox(GameObject go) {
        string type = null;
        return GetBombBoundingBox(go, ref type);
    }

    protected Rect GetBombBoundingBox(GameObject go, ref string type) {
        MeshWrapper mesh = GetBombMesh(go);
        if (mesh.IsEmpty())
            return new Rect(-1, -1, -1, -1);

        return GetBoundingBox(go, mesh.renderer, mesh.meshFilter, Camera.main, ref type);
    }


    protected Type GetTypeInAssembly(Assembly asm, string typeName) {
        Type type = asm.GetType(typeName);
        if (type==null)
            throw new InvalidOperationException(String.Format("Invalid type {0} in assembly {1}", typeName, asm.GetName().Name));
        
        return type;
    }

    protected T FindObjectInAssembly<T>(Assembly asm, string typeName) where T : UnityEngine.Object {
        Type type = GetTypeInAssembly(asm, typeName);
            
        return (T)GameObject.FindObjectOfType(type);
    }

    protected void SetFieldValue(UnityEngine.Object o, Type type, string fieldName, object value,
            BindingFlags flags=BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) {
    
        FieldInfo field = type.GetField(fieldName, flags);
        if (field==null)
             throw new InvalidOperationException(String.Format("Invalid field {0} for type {1} in object {2}", fieldName, type.FullName, o.name));
        
        field.SetValue(o, value);
    }

    protected T GetFieldValue<T>(UnityEngine.Object o, Type type, string fieldName, 
            BindingFlags flags=BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) {
    
        FieldInfo field = type.GetField(fieldName, flags);
        if (field==null)
             throw new InvalidOperationException(String.Format("Invalid field {0} for type {1} in object {2}", fieldName, type.FullName, o.name));
        
        return (T)field.GetValue(o);
    }

    protected T CallMethod<T>(UnityEngine.Object o, Type type, string methodName, 
                Type[] methodSignature=null, object[] methodArgs=null,
                BindingFlags flags=BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) {

            MethodInfo method;
            
            if (methodSignature!=null) {
                method = type.GetMethod(methodName, flags, null, methodSignature, null);
            } else { 
                method = type.GetMethod(methodName, flags);
            }

            if (method==null)
                throw new InvalidOperationException(String.Format("Invalid method {0} for type {1} in object {2}", methodName, type.FullName, o.name));

            
            if (methodArgs!=null) {
                return (T)method.Invoke(o, methodArgs);
            } else {
                return (T)method.Invoke(o, new object[]{});
            }
    }

    protected List<string> GetAllMethods(Type type, BindingFlags flags=BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) {
        List<string> output = new List<string>();

        foreach (var method in type.GetMethods(flags))
        {
            var parameters = method.GetParameters();
            var parameterDescriptions = string.Join
                (", ", method.GetParameters()
                            .Select(x => x.ParameterType + " " + x.Name)
                            .ToArray());
            
            output.Add(String.Format("Name: {0} | ReturnType: {1} | ParametersDescription: {2}", method.Name, method.ReturnType.ToString(), parameterDescriptions.ToString()));
        }

        return output;
    }

    protected object CallGenericMethod(string methodName, Type type, object[] args=null) {
        var method = this.GetType().GetMethod(methodName);
        var genericMethod = method.MakeGenericMethod(type);

        if (args!=null) {
            return genericMethod.Invoke(this, args);
        } else {
            return genericMethod.Invoke(this, new object[] {});
        }
    }

    protected Dictionary<string, GameObject> FindByNameInObject(GameObject target, HashSet<string> names) {
        Dictionary<string, GameObject> output = new Dictionary<string, GameObject>();

        var objects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => names.Contains(obj.name) && obj.transform.root.gameObject.Equals(target)); // consuming call
        foreach(GameObject go in objects) {

            output.Add(go.name, go);
        }

        return output;
    }

    protected Dictionary<string, GameObject> FindByNameInObject(GameObject target, IEnumerable<string> names) {
        HashSet<string> namesSet = new HashSet<string>();

        foreach(string name in names) {
            namesSet.Add(name);
        }

        return FindByNameInObject(target, namesSet);
    }

    protected Dictionary<string, GameObject> FindByNameInObject(GameObject target, string name) {

        return FindByNameInObject(target, new HashSet<string>() { name });
    }

    IEnumerator bombTransformAnimationWaiter(Transform transform, Action callback = null)
    {   
        var lastPos = transform.position;
        var lastRot = transform.rotation;
        var lastScal = transform.lossyScale;
        var lastMat = transform.localToWorldMatrix;

        yield return new WaitForSeconds(1);

        while (true) {
            bool b1 = ((lastPos == transform.position) && (lastRot == transform.rotation) && (lastScal == transform.lossyScale));
            bool b2 = (lastMat == transform.localToWorldMatrix);

            bool transformsAreSame = (b1 == b2);
            if (transformsAreSame)
                break;

            lastPos = transform.position;
            lastRot = transform.rotation;
            lastScal = transform.lossyScale;
            lastMat = transform.localToWorldMatrix;
            
            yield return new WaitForSeconds(2);
        }

        if (callback!=null) {
            callback();
        }

    }

    protected GameObject FindFoam(string name) {
        GameObject go = GameObject.Find(String.Format("Foam_{0}", name)); // if object is disabled will return null
        if (go==null) {
            go = GameObject.Find(String.Format("Foam_{0}_hole", name));
        }

        if (go==null)
            throw new InvalidOperationException(String.Format("Foam object {0} not found", name));

        return go;
    }

    protected string InitGameScene(bool reset=false) {

        if (sceneData.sceneManager==null || reset) {
            sceneData.sceneManager = FindObjectInAssembly<Component>(sceneData.gameAssembly, "SceneManager");
        }

        if (sceneData.facilityRoom==null || reset) {
            sceneData.facilityRoom = FindObjectInAssembly<Component>(sceneData.gameAssembly, "FacilityRoom");
        }

        if (reset) {
            bombData.foamTexture = null;
        }

        if(bombData.bomb==null || reset) {
            bombData.bomb = FindObjectInAssembly<Component>(sceneData.gameAssembly, "Bomb");
            CallMethod<object>(bombData.bomb, bombData.bomb.GetType(), "BombSolved"); // stop timer


            bombData.modulesMap = new Dictionary<string, Component>();
            bombData.modules = new List<Component>();

            Type selectableType = GetTypeInAssembly(sceneData.gameAssembly, "Selectable");

            IEnumerable bombComponentsValue = GetFieldValue<IEnumerable>(bombData.bomb, bombData.bomb.GetType(), "BombComponents");
            foreach(object item in bombComponentsValue) {
                Component bombComponent = (Component)item;

                bombData.modulesMap[bombComponent.name] = bombComponent;
                bombData.modules.Add(bombComponent);
            }

            bombData.modulesTable = new Dictionary<string, GameObject>() {
                //rear face
                { BombData.PanelPosition.ToIndex(0, 0, 0), FindFoam("B1") },
                { BombData.PanelPosition.ToIndex(0, 0, 1), FindFoam("B2") },
                { BombData.PanelPosition.ToIndex(0, 0, 2), FindFoam("B3") },
                { BombData.PanelPosition.ToIndex(0, 1, 0), FindFoam("B4") },
                { BombData.PanelPosition.ToIndex(0, 1, 1), FindFoam("B5") },
                { BombData.PanelPosition.ToIndex(0, 1, 2), FindFoam("B6") },

                // front face
                { BombData.PanelPosition.ToIndex(1, 0, 0), FindFoam("F1") },
                { BombData.PanelPosition.ToIndex(1, 0, 1), FindFoam("F2") },
                { BombData.PanelPosition.ToIndex(1, 0, 2), FindFoam("F3") },
                { BombData.PanelPosition.ToIndex(1, 1, 0), FindFoam("F4") },
                { BombData.PanelPosition.ToIndex(1, 1, 1), FindFoam("F5") },
                { BombData.PanelPosition.ToIndex(1, 1, 2), FindFoam("F6") },
            };

            if (bombData.floatingHoldable==null || reset) {
                Type floatingHoldableType = GetTypeInAssembly(sceneData.gameAssembly, "FloatingHoldable");
                bombData.floatingHoldable = bombData.bomb.gameObject.GetComponent(floatingHoldableType);
                if (bombData.floatingHoldable==null)
                    throw new InvalidOperationException(String.Format("Cannot retrieve 'FloatingHoldable' from type {0}", floatingHoldableType.FullName)); 
            }


            Dictionary<string, GameObject> elems = FindByNameInObject(bombData.bomb.gameObject, new string[] { "BombVisual", "WidgetAreas" });
            bombData.visual = elems["BombVisual"];
            bombData.visualTransform = bombData.visual.transform.parent.gameObject;
            bombData.widgetAreas = elems["WidgetAreas"];
            
            string id1 = coroutineManager.Register(delegate () { 
                CallMethod<object>(bombData.floatingHoldable, bombData.floatingHoldable.GetType(), "Resume");
            });

            string id2 = coroutineManager.Register(bombTransformAnimationWaiter(bombData.bomb.transform));

            string id3 = coroutineManager.Register(delegate () { 
                CallMethod<object>(bombData.floatingHoldable, bombData.floatingHoldable.GetType(), "Hold");
            });

            string id4 = coroutineManager.Register(bombTransformAnimationWaiter(bombData.bomb.transform));

            string id5 = coroutineManager.Register(delegate () { 
                CallMethod<object>(bombData.floatingHoldable, bombData.floatingHoldable.GetType(), "Pause");
                MapComponentWithBacking(); 
            });

            coroutineManager.Link(new string[] { id1, id2, id3, id4, id5 });
            coroutineManager.QueueStart(id1);

            return id5;
        }

        return null;
    }

    protected bool IsRectError(Rect r) {
        return (r.x<0) && (r.y<0) && (r.width<0) && (r.height<0);
    }

    protected void HighlightFoam(IEnumerable<HighlightableData> list) {
        foreach(var item in list) {
            HighlightFoam(item.go, item.color);
        }
    }

    protected Material GetFoamMaterial(GameObject go) {
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer==null)
            throw new InvalidOperationException(String.Format("Invalid renderer for object {0}", go.name));

        Material material = renderer.material;
        if (material==null)
            throw new InvalidOperationException(String.Format("Invalid material for renderer in object {0}", go.name));
        
        return material;
    }

    protected void HighlightFoam(GameObject go, Color color) {
        Material material = GetFoamMaterial(go);

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var fillColorArray =  texture.GetPixels();
        for(var i = 0; i < fillColorArray.Length; ++i)
        {
            fillColorArray[i] = color;
        }
        texture.SetPixels(fillColorArray);
        texture.Apply();

        if (bombData.foamTexture==null) {
            bombData.foamTexture = (Texture2D)material.GetTexture("_MainTex");
        }
        material.SetTexture("_MainTex", texture);
    }
    
    protected void HideFoam(IEnumerable<GameObject> goList){
        foreach(var go in goList) {
            HideFoam(go);
        }
    }

    protected void HideFoam(GameObject go){

        if (bombData.foamTexture==null)
            return ;
        
        Material material = GetFoamMaterial(go);

        material.SetTexture("_MainTex", bombData.foamTexture);
    }

    private IEnumerator HighlightFoamLauncher(GameObject go, Color color, Action callback=null) {

        yield return new WaitForEndOfFrame();

        HighlightFoam(go, color);

        if (callback!=null) {
            callback();
        }
    }

    private IEnumerator HighlightFoamLauncher(IEnumerable<HighlightableData> list, Action callback=null) {

        yield return new WaitForEndOfFrame();

        HighlightFoam(list);

        if (callback!=null) {
            callback();
        }
    }


    private IEnumerator HideFoamLauncher(GameObject go, Action callback=null) {

        yield return new WaitForEndOfFrame();

        HideFoam(go);

        if (callback!=null) {
            callback();
        }
    }

    private IEnumerator HideFoamLauncher(IEnumerable<GameObject> list, Action callback=null) {

        yield return new WaitForEndOfFrame();

        HideFoam(list);

        if (callback!=null) {
            callback();
        }
    }
    
    protected void MapComponentWithBacking() {
        var previousBombRotation = bombData.bomb.transform.rotation;
        bombData.bomb.transform.rotation = Quaternion.Euler(270, 0, 0);


        bombData.modulesPanelPosition = new Dictionary<int, BombData.PanelPosition>();
        bombData.modulesIdMap = new Dictionary<int, Component>();
        bombData.modulesPanelPositionMap = new Dictionary<string, Component>();
        bombData.modulesFront = new List<Component>();
        bombData.modulesRear = new List<Component>();
        
        Dictionary<int, Vector2> boxCenters = new Dictionary<int, Vector2>();
        Dictionary<int, Vector3> componentCenter = new Dictionary<int, Vector3>();
        Dictionary<int, Component> componentIds = new Dictionary<int, Component>();
        Dictionary<int, GameObject> componentPositions = new Dictionary<int, GameObject>();
        
        GameObject frontFace = GameObject.Find("FrontFace");
        GameObject readFace = GameObject.Find("RearFace");

        List<float> xArray = new List<float>();
        float yAverage = 0;
        float yMin = float.MaxValue;
        float yMax = 0;
        float xMin = float.MaxValue;
        float xMax = 0;
        int numberRow = 2;
        int numberColumn = 3;
        float rowSize = 0;
        float columnSize = 0;
        float zMiddle = (readFace.transform.position.z + frontFace.transform.position.z)/2;
        float width = 0;
        float height = 0;

        foreach(Component component in bombData.modules) {
            int face;
            int instanceId = component.gameObject.GetInstanceID();


            if (component.transform.position.z > zMiddle) {
                face = 1;
                bombData.modulesFront.Add(component);
            } else {
                face = 0;
                bombData.modulesRear.Add(component);
            }
            

            Rect box = GetBombComponentBoundingBox(component.gameObject);
            if (IsRectError(box)) {
                throw new InvalidOperationException(String.Format("Invalid bounding box for the bomb module {0}, error {1}", component.name, box.x));
                continue;
            }

            Vector2 pt = box.center;
            boxCenters.Add(instanceId, pt);

            yMin = Math.Min(yMin, box.y);
            xMin = Math.Min(xMin, box.x);

            height += box.height;
            width += box.width;

            xArray.Add(pt.x);

            componentCenter.Add(component.gameObject.GetInstanceID(), new Vector3(pt.x, pt.y, face));
            componentIds.Add(component.gameObject.GetInstanceID(), component);
        }


        height /= bombData.modules.Count;
        width /= bombData.modules.Count;
        xArray.Sort(); // order by column
        
        Type selectableType = GetTypeInAssembly(sceneData.gameAssembly, "Selectable");
        foreach(var item in componentCenter) {
            int instanceId = item.Key;
            Vector3 value = item.Value;

            Component component = componentIds[instanceId];

            int row = -1;
            int column = -1;
            int face = (int)value.z;

            for(int i=numberRow-1; i>=0; i--) {
                if(value.y >= yMin + height*i) {
                    row = i;
                    break;
                }
            }

            for(int i=numberColumn-1; i>=0; i--) {
                if(value.x >= xMin + width*i) {
                    column = i;
                    break;
                }
            }

            if (face==1) {
                column = numberColumn - column - 1;
            }

            string tableIndex = BombData.PanelPosition.ToIndex(face, row, column);
            GameObject foam = bombData.modulesTable[tableIndex];
            componentPositions.Add(instanceId, foam);

            bombData.modulesPanelPosition.Add(instanceId, new BombData.PanelPosition(face, row, column));
            bombData.modulesIdMap.Add(instanceId, component);
            bombData.modulesPanelPositionMap.Add(tableIndex, component);
        }

        bombData.bomb.transform.rotation = previousBombRotation;
    
    }

    protected void ResetGameState() {
        sceneData.sceneManager = null;
        sceneData.facilityRoom = null;
        bombData.bomb = null;
        bombData.floatingHoldable = null;
        bombData.modulesMap = null;
        bombData.modulesIdMap = null;
        bombData.modules = null;
    }

    private void SetGameSceneVisibility(bool visible) {
        if (sceneData.isGameSceneOn==visible) {
            return;
        }

        foreach (Transform child in sceneData.facilityRoom.transform) {
            if(child.name == "CameraAnimator") { //avoid camera backward movement when gameObject re-enabled
                continue;
            }

            child.gameObject.SetActive(visible);
        }
    }

    protected void SetGameSceneOn() {
        SetGameSceneVisibility(true);
    }


    protected void SetGameSceneOff() {
        SetGameSceneVisibility(false);
    }

    protected void HighlightRenderer(List<Renderer> renderers) {
        Color color = new Color(0, 1, 0, 1);

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGB24, false);
        var fillColorArray =  texture.GetPixels();
        for(var i = 0; i < fillColorArray.Length; ++i)
        {
            fillColorArray[i] = color;
        }
        texture.SetPixels(fillColorArray);
        texture.Apply();

        foreach(Renderer r in renderers) {
            Material m = r.material;
            if (m!=null) {
                m.SetColor("_Color", color);
                m.SetTexture("_MainTex", texture);
            }
        }
    }

    protected void HighlightModules(List<BombData.PanelPosition> posList, List<Color> colorList, Action callback=null) {
        List<GameObject> goList = posList.Select(i => bombData.modulesTable[i.ToIndex()]).ToList();

        var list = new List<HighlightableData>();

        for(int i=0; i<posList.Count; i++) {
            list.Add(new HighlightableData(goList[i], colorList[i]));
        }
        HighlightModules(list, callback);
    }

    protected void HighlightModules(List<HighlightableData> list, Action callback=null) {
        SetGameSceneOff();
        List<Renderer> renderers = new List<Renderer>();
        foreach(Component component in bombData.modules) {
            component.gameObject.SetActive(false);
            bombData.widgetAreas.SetActive(false);

        }

        coroutineManager.Start(HighlightFoamLauncher(list, callback));
    }


    protected void HideModules(List<BombData.PanelPosition> posList, Action callback=null) {
        List<GameObject> goList = posList.Select(i => bombData.modulesTable[i.ToIndex()]).ToList();

        coroutineManager.Start(HideFoamLauncher(goList, callback));
    }

    public void InitGameTracking(bool reset=false, Action callback = null) {
        string lastJobId = InitGameScene(reset);
        if (lastJobId!=null) {

            if (callback!=null) {
                string id1 = coroutineManager.Register(callback);
                coroutineManager.Link(lastJobId, id1);
            }
            coroutineManager.Start();
        }
    }

    private object FormatModuleBoundingBox(Rect rect, string type) {
        return  new {
            x = FormatNumber(rect.x),
            y = FormatNumber(rect.y),
            width = FormatNumber(rect.width),
            height = FormatNumber(rect.height),
            type = type,
        };
    }

    protected List<object> GetModulesBoundingBox(List<int> instanceIds) {
        var list = new List<object>();

        foreach(int instanceId in instanceIds) {
            Component component = bombData.modulesIdMap[instanceId];
            string type = null;
            Rect rect = GetBombComponentBoundingBox(component.gameObject, ref type);

            list.Add(new {
                id = instanceId,
                boundingBox = FormatModuleBoundingBox(rect, type),
            });
        }

        return list;
    }

    protected List<object> GetModulesBoundingBox(List<BombData.PanelPosition> posList) {
        var list = new List<object>();

        foreach(BombData.PanelPosition pos in posList) {
            string index = pos.ToIndex();
            Component component = bombData.modulesPanelPositionMap[index];
            string type = null;
            Rect rect = GetBombComponentBoundingBox(component.gameObject, ref type);

            list.Add(new {
                gridPos = index,
                boundingBox = FormatModuleBoundingBox(rect, type),
            });
        }

        return list;
    }



    private WebRequestData ParseRequest(HttpListenerRequest request) {
        if (request==null)
            return new WebRequestData();
        
        string text = null;
        using (var reader = new StreamReader(request.InputStream,
                                            request.ContentEncoding))
        {
            text = reader.ReadToEnd();
        }

        if (text==null || text.Length==0)
            return new WebRequestData();

        try {
            var data = JsonConvert.DeserializeObject<WebRequestData>(text);
            data.rawResponse = text;

            return data;
        }catch(Exception e) {
            return new WebRequestData(text);
        }
    }

    private void HighlightModulesResponse(WebRequestData requestData, WebHook webHook) {
        if (!bombInfo.IsBombPresent()) {
            throw new GameTrackingException("Bomb not present", 1);
        }
        if (bombData.bomb==null) {
            throw new GameTrackingException("Tracking env not initialized", 2);
        }

        var posList = new List<BombData.PanelPosition>();
        var colorList = new List<Color>();

        foreach(WebRequestData.HighlightModule item in requestData.highlightModules) {
            posList.Add(new BombData.PanelPosition(item.face, item.row, item.col));
            colorList.Add(new Color(item.color.r, item.color.g, item.color.b, item.color.a));
        }

        if (requestData.reset) {
            HideModules(BombData.gridPositions, delegate() {
                HighlightModules(posList, colorList, delegate() {
                    webHook.ModulesHighlighted(posList);
                });
            });
        } else {
            HighlightModules(posList, colorList, delegate() {
                webHook.ModulesHighlighted(posList);
            });
        }
    }


    private void HideModulesResponse(WebRequestData requestData, WebHook webHook) {
        if (!bombInfo.IsBombPresent()) {
            throw new GameTrackingException("Bomb not present", 1);
        }
        if (bombData.bomb==null) {
            throw new GameTrackingException("Tracking env not initialized", 2);
        }
        
        var posList = new List<BombData.PanelPosition>();

        foreach(BombData.PanelPosition item in requestData.moduleGridPositions) {
            posList.Add(new BombData.PanelPosition(item.face, item.row, item.col));
        }

        HideModules(posList, delegate() {
            webHook.ModulesHidden(posList);
        });
    }

    private void ScreenshotResponse(WebRequestData requestData, WebHook webHook) {
        StartCoroutine(TakeScreenShot((image => {
            webHook.UploadScreenshot(image, requestData.resourceId);
        })));
    }

    private void BombTransformResponse(WebRequestData requestData, WebHook webHook) {
        if (!bombInfo.IsBombPresent()) {
            throw new GameTrackingException("Bomb not present", 1);
        }
        if (bombData.bomb==null) {
            throw new GameTrackingException("Tracking env not initialized", 2);
        }

        Transform bombTransform = bombData.bomb.transform;
        if (requestData.rotation!=null) {
            
            
            if (requestData.rotation.relativeTo==null) {
                if (requestData.rotation.format=="quaternion") {
                    bombData.bomb.transform.rotation = new Quaternion(requestData.rotation.x, requestData.rotation.y, requestData.rotation.z, requestData.rotation.w);
                } else {
                    bombData.bomb.transform.rotation = Quaternion.Euler(requestData.rotation.x, requestData.rotation.y, requestData.rotation.z);
                }
            } else {
                bombData.bomb.transform.Rotate(new Vector3(requestData.rotation.x, requestData.rotation.y, requestData.rotation.z), 
                    ((requestData.rotation.relativeTo.Equals("self")) ? Space.Self : Space.World));
            }
        }

        if (requestData.position!=null) {
            var pos = new Vector3(requestData.position.x, requestData.position.y, requestData.position.z);
            if (requestData.position.relativeTo==null) {
                bombData.bomb.transform.position = pos;
            } else {
                bombData.bomb.transform.Translate(pos, ((requestData.position.relativeTo.Equals("self")) ? Space.Self : Space.World));
            }
        }

    }

    private object BombTransformResponseGet(WebRequestData requestData, WebHook webHook) {
        if (!bombInfo.IsBombPresent()) {
            throw new GameTrackingException("Bomb not present", 1);
        }
        if (bombData.bomb==null) {
            throw new GameTrackingException("Tracking env not initialized", 2);
        }

        var rot = bombData.bomb.transform.rotation;
        var pos = bombData.bomb.transform.position;

        return new {
            rotation = new {
                x = FormatNumber(rot.x),
                y = FormatNumber(rot.y),
                z = FormatNumber(rot.z),
                w = FormatNumber(rot.w),
            },
            position = new {
                x = FormatNumber(pos.x),
                y = FormatNumber(pos.y),
                z = FormatNumber(pos.z),
            }
        };
    }


    private List<object> ModulesBoundingBox(WebRequestData requestData, WebHook webHook) {
        if (!bombInfo.IsBombPresent()) {
            throw new GameTrackingException("Bomb not present", 1);
        }
        if (bombData.bomb==null) {
            throw new GameTrackingException("Tracking env not initialized", 2);
        }

        List<object> list;

        if (requestData.moduleGridPositions!=null) {
            logger.Log("enter");
            logger.Log("list="+requestData.moduleIds);
            list = GetModulesBoundingBox(requestData.moduleGridPositions);
        } else {
            list = GetModulesBoundingBox(requestData.moduleIds);
        }

        return list;
    }

    private void InitGameTrackingResponse(WebRequestData requestData, WebHook webHook) {

        if (!bombInfo.IsBombPresent()) {
            throw new GameTrackingException("Bomb not present", 1);
        }

        InitGameTracking(requestData.reset, delegate() {

            var data = new List<object>();

            foreach(Component module in bombData.modules) {
                int instanceId = module.gameObject.GetInstanceID();
                var pos = bombData.modulesPanelPosition[instanceId];

                var components = module.gameObject.GetComponents(typeof(Component));
                var componentsList = new List<object>();
                foreach(Component component in components) {
                    Type compType = component.GetType();

                    var item = new {
                        name = component.name,
                        type = new {
                            name = compType.Name,
                            fullName = compType.FullName,
                            nameSpace = compType.Namespace,
                            assembly = new {
                                fullName = compType.Assembly.FullName,
                                qualifiedName =  compType.AssemblyQualifiedName,
                                version = compType.Assembly.GetName().Version,
                                info = compType,
                            },
                        }
                    };

                    componentsList.Add(item);
                }

                Type moduleType = module.GetType();
                var mType = new {
                    name = moduleType.Name,
                    fullName = moduleType.FullName,
                    nameSpace = moduleType.Namespace,
                    assembly = new {
                        fullName = moduleType.Assembly.FullName,
                        qualifiedName =  moduleType.AssemblyQualifiedName,
                        version = moduleType.Assembly.GetName().Version,
                        info = moduleType,
                    },
                };

                data.Add(new {
                    id = instanceId,
                    name = module.gameObject.name,
                    type = mType,
                    components = componentsList,
                    gridPosition = new {
                        face = pos.face,
                        col = pos.col,
                        row = pos.row,
                    },
                });
            }

            webHook.GameTrackingInitiated(new { bombModules = data });
        });
    }

    private string HttpListenerResponse(HttpListenerRequest request, WebRequestData data, HttpListenerResponse response, WebHook webHook) {

        var uri = new Uri(request.Url.OriginalString);
        logger.Log("request.Url.OriginalString="+request.Url.OriginalString);
        logger.Log("request.HttpMethod="+request.HttpMethod);

        if (uri.Segments.Length<=1) {
            return "Ktane tracking mod server is up";
        }

        if (uri.Segments[1].Equals(Uri.EscapeUriString("clearLog")))
        {
            logger.Clear();
            return "ok";
        }

        if (uri.Segments[1].Equals(Uri.EscapeUriString("initGameTracking")))
        {
            if (!request.HttpMethod.Equals("POST")) {
                response.StatusCode = 405;
                return "method not allowed";
            }

            InitGameTrackingResponse(data, webHook);

            return "ok";
        }

        if (uri.Segments[1].Equals(Uri.EscapeUriString("highlightModules")))
        {
            if (!request.HttpMethod.Equals("POST")) {
                response.StatusCode = 405;
                return "method not allowed";
            }

            HighlightModulesResponse(data, webHook);

            return "ok";
        }

        if (uri.Segments[1].Equals(Uri.EscapeUriString("hideModules")))
        {
            if (!request.HttpMethod.Equals("POST")) {
                response.StatusCode = 405;
                return "method not allowed";
            }

            HideModulesResponse(data, webHook);

            return "ok";
        }

        if (uri.Segments[1].Equals(Uri.EscapeUriString("screenshot")))
        {
            if (!request.HttpMethod.Equals("POST")) {
                response.StatusCode = 405;
                return "method not allowed";
            }

            ScreenshotResponse(data, webHook);

            return "ok";
        }

        if (uri.Segments[1].Equals(Uri.EscapeUriString("bombTransform")))
        {
            if (request.HttpMethod.Equals("GET")) {
                response.ContentType = "application/json";

                return JsonConvert.SerializeObject( new {
                    data = new {
                        transform = BombTransformResponseGet(data, webHook),
                    },
                });

            } else if (request.HttpMethod.Equals("POST")) {
                BombTransformResponse(data, webHook);
                return "ok";

            } else {
                response.StatusCode = 405;
                return "method not allowed";
            }
        }

        if (uri.Segments[1].Equals(Uri.EscapeUriString("modulesBoundingBox")))
        {
            if (!request.HttpMethod.Equals("POST")) { //should be a GET but identifiers may not fit in the url max length
                response.StatusCode = 405;
                return "method not allowed";
            }
            response.ContentType = "application/json";

            return JsonConvert.SerializeObject( new {
                data = new {
                    boundingBoxes = ModulesBoundingBox(data, webHook),
                },
            });
        }

        response.StatusCode = 404;
        return "NOT FOUND";
    }

    public void SimpleListenerExample(HttpListener listener, WebHook webHook) {
        while (true)
        {
            // Note: The GetContext method blocks while waiting for a request. 
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            WebRequestData requestData = ParseRequest(request);
            logger.Log("raw response="+requestData.rawResponse);

            HttpListenerResponse response = context.Response;
            response.ContentType = "text/html";
            //response.ContentType = "application/json";

            string responseString = "";

            try {
                responseString = HttpListenerResponse(request, requestData, response, webHook);

            } catch(GameTrackingException e) {
                var resp = new {
                    error = true,
                    errorCode = e.errorCode,
                    errorType = e.GetType(),
                    errorMessage = e.Message,
                    errorStack = e.StackTrace,
                };
                response.ContentType = "application/json";
                response.StatusCode = 403;
                responseString = JsonConvert.SerializeObject(resp);

                logger.Log(String.Format("Exception<{0}> : {1} [{2}]", e.GetType(), e.Message, e.errorCode));
                logger.Log(String.Format("Exception stack trace : {0}", e.StackTrace));
            
            } catch(Exception e) {
                var resp = new {
                    error = true,
                    errorType = e.GetType(),
                    errorMessage = e.Message,
                    errorStack = e.StackTrace,
                };
                response.ContentType = "application/json";
                response.StatusCode = 403;
                responseString = JsonConvert.SerializeObject(resp);

                logger.Log(String.Format("Exception<{0}> : {1}", e.GetType(), e.Message));
                logger.Log(String.Format("Exception stack trace : {0}", e.StackTrace));
            }
            
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            
            System.IO.Stream output = response.OutputStream;
            try {
                output.Write(buffer, 0, buffer.Length);
            
            } finally {
                output.Close();
            }
        }
    }
  
    public class Worker
    {
        private ExampleWebService service;
        private HttpListener listener;
        private WebHook webHook;
        private Logger logger;
        private string address;
        
        public Worker(ExampleWebService s)
        {
            logger = new Logger("ExampleWebService", true);
            service = s;
            webHook = new WebHook(service.settings.webHookAddr);
        }

        public void DoWork()
        {   
            listener = new HttpListener();
        
            try {
                for(int port = service.settings.portRange[0]; port<service.settings.portRange[1]+1; port++) {
                    string addr = String.Format("{0}://{1}:{2}/", service.settings.method, service.settings.host, port);
                    try {

                        listener.Prefixes.Add(addr);
                        listener.Start();
                        logger.Log(String.Format("Server started at {0}", addr));
                        webHook.Register(addr);
                        
                        service.SimpleListenerExample(listener, webHook);
                        break;

                    } catch (System.Net.Sockets.SocketException e) {
                        if (e.ErrorCode == 10048) {
                            continue;
                        }

                        throw e;
                    } finally {
                        listener.Prefixes.Remove(addr);
                    }
                }
            
            }catch(Exception e) {
                string addr = TrackingModeSettings.defaultAddr;
                listener.Prefixes.Add(addr);
                listener.Start();
                logger.Log(String.Format("Server started at {0}", addr));
                webHook.Register(addr);
                service.SimpleListenerExample(listener, webHook);
            }
            
        }

        public void Stop()
        {
            listener.Stop();
        }
    }

    public class HighlightableData {
        public GameObject go;
        public Color color;

        public HighlightableData(GameObject go, Color color) {
            this.go = go;
            this.color = color;
        }
    }
}