using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//[ExecuteInEditMode]
namespace DrivingSimulatorScene.CameraRig
{
    public class CameraRigManager : MonoBehaviour
    {


        [Header("UserInput")]
        [SerializeField] private int m_screenCount;
        [SerializeField] private float m_distanceOfDriverToScreens;
        [SerializeField] private float m_widthOfSingleScreen;
        [SerializeField] private Vector2 m_screensResolution;




        [Header("Debug Fields")]
        [SerializeField] private float m_aspectRatio;
        [SerializeField] private List<Camera> m_cameras = new List<Camera>();
        [SerializeField] private List<Graphics> m_fadingImage = new List<Graphics>();




        void Start()
        {

            Debug.Log("displays connected: " + Display.displays.Length);

            
            for (int i = 1; i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
            }

            UpdateScreenSetupWindows();
        }


        //for testing purposes
        [ContextMenu(("UpdateFOV"))]
        public void UpdateScreenSetup()
        {
            Debug.Log("UpdateScreenSetup");

            m_aspectRatio = m_screensResolution.x / m_screensResolution.y;
            ClearCameras(); //destroy old cameras
            CreateCameras(m_screenCount); //create one camera object for each screen of the driving sim (exclude operator screen and HMI)
            CalculateFoVforAllCameras(m_cameras, m_widthOfSingleScreen, m_aspectRatio, m_distanceOfDriverToScreens);
        }

        [ContextMenu(("UpdateFOV Windows Cam Setup"))]
        public void UpdateScreenSetupWindows()
        {
            Debug.Log("UpdateScreenSetupWindows");

            m_aspectRatio = m_screensResolution.x / m_screensResolution.y;
            ClearCameras(); //destroy old cameras
            //CreateCamerasForWindowsScreenOrder(_drivingSimScreenConfiguration.WindowsScreenOrder.ToArray()); //create one camera object for each screen of the driving sim (exclude operator screen and HMI)
            CalculateFoVforAllCameras(m_cameras, m_widthOfSingleScreen, m_aspectRatio, m_distanceOfDriverToScreens);
        }

        //Use this Method to Create Cameras from another script
        void UpdateScreenSetup(Vector2 screensResolution, int screenCount, float widthOfSingleScreen, float distanceOfDriverToScreens)
        {
            m_aspectRatio = screensResolution.x / screensResolution.y;
            ClearCameras(); //destroy old cameras
            CreateCameras(screenCount); //create one camera object for each screen of the driving sim (exclude operator screen and HMI)
            CalculateFoVforAllCameras(m_cameras, widthOfSingleScreen, m_aspectRatio, distanceOfDriverToScreens);

            //save into member variables
            m_screensResolution = screensResolution;
            m_screenCount = screenCount;
            m_widthOfSingleScreen = widthOfSingleScreen;
            m_distanceOfDriverToScreens = distanceOfDriverToScreens;
        }

        void ClearCameras()
        {
            for (int i = 0; i < m_cameras.Count; i++)
                Destroy(m_cameras[i].gameObject);
            m_cameras.Clear();
            /*
            for (int i = 0; i < m_fadeToBlack.Count; i++)
                Destroy(m_fadeToBlack[i].gameObject);
            m_fadeToBlack.Clear();
            */
            m_fadingImage.Clear();
        }

        void CreateCameras(int cameraCount)
        {
            for (int i = 0; i < cameraCount; i++)
            {
                var cameraGameObject = MakeChild(this.transform, "rigCam" + i);
                m_cameras.Add(GetPreparedCamera(cameraGameObject, i));

               // FadeToBlack ftb = GetPreparedFadeToBlack(i, m_cameras[i]);
               // m_fadeToBlack.Add(ftb);

            }
        }

        void CreateCamerasForWindowsScreenOrder(int[] windowsScreenOrder)
        {
            for (int i = 0; i < windowsScreenOrder.Length; i++)
            {
                var cameraGameObject = MakeChild(this.transform, "rigCam" + windowsScreenOrder[i]);
                m_cameras.Add(GetPreparedCamera(cameraGameObject, windowsScreenOrder[i]));

            }
        }

        GameObject MakeChild(Transform parent, string name)
        {
            var child = new GameObject(name);

            child.transform.SetParent(parent);
            child.transform.localPosition = Vector3.zero;
            child.transform.rotation = Quaternion.identity;

            return child;
        }

        Camera GetPreparedCamera(GameObject go, int targetDisplay)
        {
            var newCamera = go.AddComponent<Camera>();
            newCamera.targetDisplay = targetDisplay;
            newCamera.stereoTargetEye = StereoTargetEyeMask.None; //make camera mono-scopic for non VR purposes
            return newCamera;
        }

        void CalculateFoVforAllCameras(List<Camera> cameras, float widthOfSingleScreen, float aspectRatio, float distanceOfDriverToScreens)
        {
            float fieldOfView = GetFieldOfView(distanceOfDriverToScreens, widthOfSingleScreen * cameras.Count); //calculate FOV using 
            float fieldOfViewSingleCam = fieldOfView / cameras.Count;
            float tempFov = Camera.HorizontalToVerticalFieldOfView(fieldOfViewSingleCam, aspectRatio); //cache FOV for one camera, use it for all cameras
            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].fieldOfView = tempFov; //apply calculated FOV for every camera 

                //do we have an even or odd amount of cameras? we need to adjust the rotation offset accordingly                
                float rotationOffset = 0;
                if (cameras.Count % 2 == 0)
                    rotationOffset = (fieldOfViewSingleCam * cameras.Count * 0.5f) - (fieldOfViewSingleCam * 0.5f); //even amount of cameras
                else
                    rotationOffset = fieldOfViewSingleCam * Mathf.Floor(cameras.Count * 0.5f); //odd amount of cameras

                cameras[i].transform.localRotation = Quaternion.Euler(0.0f, (fieldOfViewSingleCam * i + 1) - rotationOffset, 0.0f); //apply rotation of each camera in local space
            }
        }


        //TODO move to static Utility Class
        float GetFieldOfView(float distanceOfDriverToScreens, float widthOfAllScreens)
        {
            return (widthOfAllScreens * 180) / (Mathf.PI * distanceOfDriverToScreens);
        }

    }
}
