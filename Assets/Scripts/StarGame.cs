using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

public class StarGame : MonoBehaviour
{
    [Serializable] public class Hud { public string phase = "ready"; public int score; public int lives = 3; public int time = 60; }
    class Falling { public Transform body; public bool meteor; public float speed; }
    public Transform player;
    public Camera gameCamera;
    public Hud state = new Hud();
    readonly List<Falling> falling = new List<Falling>();
    float elapsed, spawnTimer, shield, externalDirection;
    float target = -1;
    string lastHud;
    AudioSource audioSource;
    AudioClip collectSound, hitSound;
    bool sound;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void ReportState(string json);
#endif
    public float HalfWidth => Mathf.Max(2f, gameCamera.orthographicSize * gameCamera.aspect - .45f);

    public void BuildWorld()
    {
        gameCamera = Camera.main;
        if (!gameCamera) {
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            gameCamera = cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
        }
        gameCamera.transform.position = new Vector3(0, 0, -20);
        gameCamera.transform.rotation = Quaternion.identity;
        gameCamera.orthographic = true; gameCamera.orthographicSize = 7;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(.035f, .065f, .105f);
        gameCamera.nearClipPlane = .1f; gameCamera.farClipPlane = 100;
        if (player) return;
        player = new GameObject("Player Ship").transform;
        player.SetParent(transform); player.position = new Vector3(0, -5.5f, 0);
        var body = Shape("Ship", player, new Vector3(0,0,0), new Vector3(.65f,.9f,.3f), "Ship", PrimitiveType.Capsule);
        Shape("Cockpit", player, new Vector3(0,.1f,-.23f), new Vector3(.29f,.48f,.12f), "Glass", PrimitiveType.Sphere);
        var wingLeft = Shape("Left wing", player, new Vector3(-.37f,-.2f,.05f), new Vector3(.65f,.17f,.2f), "Ship", PrimitiveType.Cube);
        wingLeft.localRotation = Quaternion.Euler(0,0,35);
        var wingRight = Shape("Right wing", player, new Vector3(.37f,-.2f,.05f), new Vector3(.65f,.17f,.2f), "Ship", PrimitiveType.Cube);
        wingRight.localRotation = Quaternion.Euler(0,0,-35);
        Shape("Engine", player, new Vector3(0,-.61f,.05f), new Vector3(.2f,.4f,.12f), "Star", PrimitiveType.Capsule);
        var scenery = new GameObject("Star field").transform; scenery.SetParent(transform);
        for (int i = 0; i < 95; i++) {
            float x = Mathf.Repeat(i * 3.771f, 28) - 14;
            float y = Mathf.Repeat(i * 2.331f, 14) - 7;
            float size = i % 7 == 0 ? .035f : .018f;
            Shape("Distant star", scenery, new Vector3(x,y,3), new Vector3(size,size,.01f), "Dust", PrimitiveType.Quad);
        }
    }
    static Transform Shape(string name, Transform parent, Vector3 position, Vector3 scale, string material, PrimitiveType type)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent);
        obj.transform.localPosition = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>(material);
        var collider = obj.GetComponent<Collider>(); if (collider) collider.enabled = false;
        return obj.transform;
    }
    void Start()
    {
        Application.targetFrameRate = 60;
        BuildWorld();
        audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false;
        collectSound = Tone(800); hitSound = Tone(120);
        Publish(true);
    }
    AudioClip Tone(float frequency)
    {
        int count = 6615;
        var samples = new float[count];
        for (int i = 0; i < count; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 44100) * .12f * (1f - (float)i / count);
        var clip = AudioClip.Create("Tone", count, 1, 44100, false); clip.SetData(samples,0); return clip;
    }
    public void StartRound()
    {
        foreach (var item in falling) if (item.body) Destroy(item.body.gameObject);
        falling.Clear(); elapsed = 0; spawnTimer = .4f; shield = 0; externalDirection = 0; target = -1;
        state = new Hud { phase = "playing" }; player.position = new Vector3(0,-5.5f,0); Publish(true);
    }
    public void TogglePause()
    {
        if (state.phase == "playing") state.phase = "paused";
        else if (state.phase == "paused") state.phase = "playing";
        externalDirection = 0; target = -1; Publish(true);
    }
    public void PauseGame() { if (state.phase == "playing") TogglePause(); }
    public void SetDirection(string value) { if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) externalDirection = Mathf.Clamp(v,-1,1); target = -1; }
    public void SetTarget(string value) { if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) target = Mathf.Clamp01(v); }
    public void ReleasePointer() { target = -1; externalDirection = 0; }
    public void SetSound(string value) { sound = value == "1"; }
    void OnApplicationFocus(bool focused) { if (!focused) PauseGame(); }
    void OnApplicationPause(bool paused) { if (paused) PauseGame(); }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (state.phase != "playing") return;
        float dt = Mathf.Min(Time.deltaTime,.05f);
        elapsed += dt; shield = Mathf.Max(0,shield-dt); spawnTimer += dt;
        float direction = externalDirection;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) { direction -= 1; target = -1; }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) { direction += 1; target = -1; }
#if UNITY_EDITOR
        if (Input.GetMouseButton(0)) target = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
        if (Input.GetMouseButtonUp(0)) target = -1;
#endif
        float speed = HalfWidth * 1.4f;
        float x = target >= 0 ? Mathf.MoveTowards(player.position.x,Mathf.Lerp(-HalfWidth,HalfWidth,target),speed*dt) : player.position.x + Mathf.Clamp(direction,-1,1)*speed*dt;
        player.position = new Vector3(Mathf.Clamp(x,-HalfWidth,HalfWidth),-5.5f,0);
        player.localScale = Vector3.one * (shield > 0 ? .93f + .07f*Mathf.Sin(elapsed*30) : 1);
        if (spawnTimer >= Mathf.Lerp(.72f,.4f,elapsed/60)) { spawnTimer = 0; Spawn(); }
        for (int i = falling.Count-1; i >= 0; i--) {
            var item = falling[i];
            item.body.position += Vector3.down * item.speed * dt;
            item.body.Rotate(0, item.meteor ? 0 : 80*dt, 35*dt);
            bool hit = Vector2.Distance(item.body.position,player.position) < (item.meteor ? .69f : .64f);
            if (hit) {
                if (item.meteor) {
                    if (shield <= 0) { state.lives--; shield = 1.5f; if(sound) audioSource.PlayOneShot(hitSound); }
                } else { state.score += 10; if(sound) audioSource.PlayOneShot(collectSound); }
            }
            if (hit || item.body.position.y < -8) { Destroy(item.body.gameObject); falling.RemoveAt(i); }
        }
        state.time = Mathf.CeilToInt(Mathf.Max(0,60-elapsed));
        if (state.lives <= 0 || elapsed >= 60) { state.phase = "ended"; externalDirection = 0; target = -1; }
        Publish();
    }
    void Spawn()
    {
        bool meteor = UnityEngine.Random.value < .32f;
        var position = new Vector3(UnityEngine.Random.Range(-HalfWidth+.15f,HalfWidth-.15f),7.5f,0);
        Transform body;
        if (meteor) body = Shape("Meteor",transform,position,Vector3.one*.75f,"Meteor",PrimitiveType.Sphere);
        else {
            body = new GameObject("Star pickup").transform; body.SetParent(transform); body.position = position;
            var mesh = new Mesh();
            var vertices = new Vector3[11]; vertices[0] = Vector3.zero;
            for (int i=0;i<10;i++) { float a = i*Mathf.PI/5 + Mathf.PI/2; float r = i%2==0 ? .42f : .19f; vertices[i+1] = new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0); }
            var triangles = new int[60];
            for(int i=0;i<10;i++) { int k=i*6, a=i+1,b=(i+1)%10+1; triangles[k]=0;triangles[k+1]=b;triangles[k+2]=a; triangles[k+3]=0;triangles[k+4]=a;triangles[k+5]=b; }
            mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals();
            body.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            body.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Star");
        }
        falling.Add(new Falling {body=body,meteor=meteor,speed=UnityEngine.Random.Range(3f,3.8f)+elapsed*.035f});
    }
    void Publish(bool force=false)
    {
        string json = JsonUtility.ToJson(state); if(!force && json==lastHud)return; lastHud=json;
#if UNITY_WEBGL && !UNITY_EDITOR
        ReportState(json);
#endif
    }
    void OnGUI()
    {
#if UNITY_EDITOR || !UNITY_WEBGL
        GUI.skin.label.fontSize = 22; GUI.skin.button.fontSize = 22;
        GUI.Label(new Rect(20,20,700,45),$"STAR CATCHER    Score: {state.score}    Lives: {state.lives}    Time: {state.time}");
        if(state.phase!="playing") {
            GUI.Box(new Rect(Screen.width/2-190,Screen.height/2-75,380,150),"STAR CATCHER");
            if(GUI.Button(new Rect(Screen.width/2-160,Screen.height/2-20,320,65),state.phase=="paused"?"RESUME":"START GAME")) { if(state.phase=="paused")TogglePause();else StartRound(); }
        }
        else if(GUI.Button(new Rect(Screen.width-135,20,115,45),"PAUSE"))TogglePause();
#endif
    }
}
