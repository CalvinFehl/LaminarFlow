using Unity.Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CameraManager01Knot : MonoBehaviour
{
    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private CinemachineCamera closeupCamera;
    [SerializeField] private CinemachineCamera spawnCamera;
    [SerializeField] private float distanceThreshold = 2f, distanceUncertaintyTime = 0.3f, spawnCameraDuration = 0.5f;
    private float distanceTimer, spawnCamTimer;
    [SerializeField] private List<CinemachineCamera> cameras = new List<CinemachineCamera>();
    [SerializeField] private string activeCameraName;

    [SerializeField] private Transform player;
    [SerializeField] private Transform closeUpCameraPivot;
    [SerializeField] private float minimumDistance = 4f, countsAsZeroTreshold = 0.2f;
    private Vector3 lastCloseUpCamOffset = new Vector3(0f, 0f, 4f);


    private void OnEnable()
    {
        AimedChargedDash dash = Object.FindFirstObjectByType<AimedChargedDash>();
        if (dash != null) { dash.OnAimingCameraUpdated += SwitchCamera; }
    }

    private void OnDisable()
    {
        AimedChargedDash dash = Object.FindFirstObjectByType<AimedChargedDash>();
        if (dash != null) { dash.OnAimingCameraUpdated -= SwitchCamera; }
    }

    void Start()
    {
        var potentialCameras = GetComponentsInChildren<CinemachineCamera>().ToList();
        if (followCamera == null) followCamera = GetCameraByName("Follow", potentialCameras);
        if (aimCamera == null) aimCamera = GetCameraByName("Aim", potentialCameras);
        if (closeupCamera == null) closeupCamera = GetCameraByName("Close", potentialCameras);
        if (spawnCamera == null) spawnCamera = GetCameraByName("Spawn", potentialCameras);

        if (cameras.Count == 0)
        {
            if (followCamera != null) cameras.Add(followCamera); else Debug.LogWarning("No follow camera found");
            if (aimCamera != null) cameras.Add(aimCamera); else Debug.LogWarning("No aim camera found");
            if (closeupCamera != null) cameras.Add(closeupCamera); else Debug.LogWarning("No closeup camera found");
            if (spawnCamera != null) cameras.Add(spawnCamera); else Debug.LogWarning("No spawn camera found");
        }
        SwitchCamera("Close");
    }

    public void SwitchCamera(string cameraName = default)
    {
        if (string.IsNullOrEmpty(cameraName)) return;
        if (activeCameraName == null) activeCameraName = cameraName;
        else if (activeCameraName.Contains(cameraName)) return;

        if (cameraName.Contains("Follow")) // try switch to follow camera
        {
            followCamera.Priority = 10;

            foreach (CinemachineCamera camera in cameras)
            { if (camera != followCamera) camera.Priority = 0; }
        }
        else if (cameraName.Contains("Aim")) // try switch to aim camera
        {
            aimCamera.Priority = 20;
            GetCameraByName(activeCameraName).Priority = 0;
        }
        else if (cameraName.Contains("Close")) // try switch to closeup camera
        {
            closeupCamera.Priority = 10;
            if (activeCameraName.Contains("Follow")) followCamera.Priority = 0;
        }
        else if (cameraName.Contains("Spawn")) // try switch to spawn camera
        {
            spawnCamera.Priority = 20;
            foreach (CinemachineCamera camera in cameras)
            {
                if (camera.Name.Contains("Follow")) camera.Priority = 10;
                else if (camera != spawnCamera) camera.Priority = 0; 
            }
        }
        else { Debug.LogWarning("No camera with name: " + cameraName); return; }
        activeCameraName = cameraName;
    }

    public void LateUpdate()
    {
        UpdateTheCameras();
    }

    private void UpdateTheCameras()
    {
        if (followCamera != null && followCamera.Follow != null) // check if follow camera is set and has a target
        {
            Vector3 playerPos = new Vector3(player.position.x, 0f, player.position.z);
            Vector3 folloCamPos = new Vector3(followCamera.transform.position.x, 0f, followCamera.transform.position.z);

            float distance2D = Vector2.Distance(new Vector2(folloCamPos.x, folloCamPos.z), new Vector2(playerPos.x, playerPos.z));

            HandleCameraMinimumDistance(distance2D);
            HandleCloseUpCamera(followCamera, player);
        }

        if (spawnCamera != null && spawnCamTimer < spawnCameraDuration)
        {
            spawnCamTimer += Time.deltaTime;
            if (spawnCamTimer >= spawnCameraDuration)
            {
                spawnCamera.Priority = 0;
            }
        }
    }

    private CinemachineCamera GetCameraByName(string cameraName, List<CinemachineCamera> _cameras = null)
    {
        if (_cameras != null && _cameras.Count > 0)
        {
            return _cameras.FirstOrDefault(c => c.name.Contains(cameraName));
        }
        return cameras.FirstOrDefault(c => c.name.Contains(cameraName));
    }
    
    private void HandleCloseUpCamera(CinemachineCamera mainCamera, Transform player = null)
    {
        if (closeUpCameraPivot == null) { Debug.LogWarning("no closeUpPivot set"); return; }

        if (player == null) player = mainCamera.Follow;
        if (player == null) { return; }
        if (closeupCamera != null && mainCamera != null)
        {
            Vector3 offset = mainCamera.transform.position - player.position;

            Vector2 offset2D = new Vector2(offset.x, offset.z);
            float distance2D = offset2D.magnitude;
            if (distance2D < countsAsZeroTreshold)
            {
                offset2D = new Vector2(lastCloseUpCamOffset.x, lastCloseUpCamOffset.z);
            }
            if (distance2D < minimumDistance) 
            { 
                offset2D = offset2D.normalized * minimumDistance;
            }

            offset = new Vector3(offset2D.x, offset.y, offset2D.y);

            mainCamera.transform.position = player.position + offset;
            closeUpCameraPivot.position = player.position + offset;
            lastCloseUpCamOffset = offset;
        }
    }

    public void SetCameraBehindPlayer(Vector3 _offset = default, Transform _playerTransform = null)
    {
        if (_offset == default) _offset = new Vector3(0f, 0f, -4f);
        if (_playerTransform == null) _playerTransform = player;
        if (_playerTransform == null) { return; }

        if (spawnCamera != null)
        {
            spawnCamera.transform.position = _playerTransform.position - _playerTransform.transform.forward * 4f;
            SwitchCamera("Spawn");
            spawnCamTimer = 0f;
        }
    }

    public void HandleCameraMinimumDistance(float distance = 0f, bool isCloseToPlayer = default)
    {
        if (isCloseToPlayer == default) isCloseToPlayer = distance < distanceThreshold;

        if (distanceUncertaintyTime == 0f) SwitchCamera(isCloseToPlayer ? "Close" : "Follow");

        if (isCloseToPlayer && distanceTimer >= 0f)
        {
            if (distanceTimer == distanceUncertaintyTime) { return; }
            else if (distanceTimer > distanceUncertaintyTime)
            {
                distanceTimer = distanceUncertaintyTime;
                SwitchCamera("Close");
            }
            else { distanceTimer += Time.fixedDeltaTime; }
        }
        else if (!isCloseToPlayer && distanceTimer <= 0f)
        {
            if (distanceTimer == -distanceUncertaintyTime) { return; }
            else if (distanceTimer < -distanceUncertaintyTime)
            {
                distanceTimer = -distanceUncertaintyTime;
                SwitchCamera("Follow");
            }
            else { distanceTimer -= Time.fixedDeltaTime; }
        }
        else { distanceTimer = 0f; }
    }
}
