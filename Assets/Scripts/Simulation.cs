using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    [SerializeField] ComputeShader shader;
    [SerializeField, Range(0f, 1f)]
    float fill = 0;

    [SerializeField]
    int width = 240;
    
    int height = 135;

    int simulationKernel = 0,
        displayKernel = 1;

    bool updateDisplay = false;

    int threadsGroupX, threadsGroupY;
    public int frameCount, FPS = 10;

    RenderTexture cells;
    RenderTexture nextCells;
    RenderTexture displayTexture;

    int[] initialStateData;
    ComputeBuffer initialState;

    Material material;

    string buttonText = "Stop";
    bool running = true;

    private void Awake()
    {
        material = GetComponentInChildren<MeshRenderer>().material;
        Init();
    }

    private void FixedUpdate()
    {
        if (running)
        {
            RunSimulation();
            updateDisplay = true;
        }
    }

    private void Update()
    {
        if (updateDisplay)
        {
            UpdateDisplay();
            updateDisplay = false;
        }
    }

    void Init()
    {
        frameCount = 0;
        height = Mathf.RoundToInt(width * 9 / 16);

        Time.fixedDeltaTime = 1f / FPS;

        threadsGroupX = Mathf.CeilToInt(width / 8f);
        threadsGroupY = Mathf.CeilToInt(height / 8f);

        initialStateData = new int[width * height];
        InitialConditions();

        cells = new RenderTexture(width, height, 0);
        cells.enableRandomWrite = true;
        cells.Create();

        cells.wrapMode = TextureWrapMode.Clamp;
        cells.filterMode = FilterMode.Point;

        nextCells = new RenderTexture(width, height, 0);
        nextCells.enableRandomWrite = true;
        nextCells.Create();

        nextCells.wrapMode = TextureWrapMode.Clamp;
        nextCells.filterMode = FilterMode.Point;

        displayTexture = new RenderTexture(width, height, 0);
        displayTexture.enableRandomWrite = true;
        displayTexture.Create();

        displayTexture.wrapMode = TextureWrapMode.Clamp;
        displayTexture.filterMode = FilterMode.Point;

        shader.SetTexture(simulationKernel, "Cells", cells);
        shader.SetTexture(simulationKernel, "NextCells", nextCells);
        shader.SetTexture(displayKernel, "Cells", cells);
        shader.SetTexture(displayKernel, "DisplayTexture", displayTexture);

        shader.SetInt("width", width);
        shader.SetInt("height", height);

        material.mainTexture = displayTexture;
    }

    void InitialConditions()
    {
        for (int i = 0; i < initialStateData.Length; i++)
            initialStateData[i] = Random.value < fill ? 1 : 0;

        //int wh = Mathf.CeilToInt(width / 2);
        //int hh = Mathf.CeilToInt(height / 2);

        //initialStateData[hh * width + wh] = 1;
        //initialStateData[(hh + 1) * width + wh] = 1;
        //initialStateData[hh * width + wh + 1] = 1;
        //initialStateData[(hh + 1) * width + wh + 1] = 1;
    }

    void RunSimulation()
    {
        shader.SetInt("frameCount", frameCount);

        initialState = new ComputeBuffer(height * width, sizeof(int));
        initialState.SetData(initialStateData);
        shader.SetBuffer(simulationKernel, "InitialState", initialState);
        
        shader.Dispatch(simulationKernel, threadsGroupX, threadsGroupY, 1);

        initialState.Release();

        Graphics.Blit(nextCells, cells);

        frameCount++;
    }

    void UpdateDisplay()
    {
        if (nextCells == null)
            return;

        shader.Dispatch(displayKernel, threadsGroupX, threadsGroupY, 1);
    }

    private void ResetConditions()
    {
        frameCount = 0;
        InitialConditions();
    }

    private void OnDestroy()
    {
        displayTexture.Release();
        cells.Release();
        nextCells.Release();
    }

    private void OnGUI()
    {
        if(GUI.Button(new Rect(0,0,70,40), buttonText))
        {
            running = !running;
            buttonText = running ? "Stop" : "Resume";
        }
        if (GUI.Button(new Rect(75, 0, 70, 40), "Reset"))
        {
            ResetConditions();
        }
    }
}
