using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public class LevelSelector : MonoBehaviour
    {
        public static string MAX_LEVEL_KEY = "maxLevel";
        private Level[] levels;
        public Level previewingLevel;

        public GameObject leftButton;
        public GameObject rightButton;
        public GameObject clearButton;
        public GameObject quitButton;

        private void UpdateButtons()
        {
            leftButton.SetActive(previewingLevel.index != 0);
            rightButton.SetActive(previewingLevel.index < Math.Min(levels.Length - 1, PlayerPrefs.GetInt("maxLevel", 0)));
            clearButton.SetActive(PlayerPrefs.GetInt("maxLevel", 0) > 0);
            quitButton.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);
        }
        
        private void OnEnable()
        {
            if (levels == null || levels.Length == 0)
            {
                Level[] levelsUnsorted = FindObjectsOfType<Level>(true);
                levels = new Level[levelsUnsorted.Length];
                foreach (Level level in levelsUnsorted)
                {
                    if (levels[level.index] != null) Debug.LogError("Duplicate level index");
                    levels[level.index] = level;
                }
            }
            PreviewLevel(Math.Min(levels.Length - 1, PlayerPrefs.GetInt("maxLevel", 0)));

            DraggableAlongSpline.AllowDragging = false;
            UpdateButtons();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                GoLeft();
            }
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                GoRight();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                                                 || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow)
                                                 || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
            {
                Select();
            }
        }

        public void GoLeft()
        {
            int newIndex = previewingLevel.index - 1;
            if (newIndex < 0) return;
            PreviewLevel(newIndex);
        }
        
        public void GoRight()
        {
            int newIndex = previewingLevel.index + 1;
            if (newIndex > PlayerPrefs.GetInt(MAX_LEVEL_KEY, 0) || newIndex > levels.Length - 1) return;
            PreviewLevel(newIndex);
        }

        public void Select()
        {
            previewingLevel.StartHere();
            gameObject.SetActive(false);
            DraggableAlongSpline.AllowDragging = true;
        }

        public void PreviewLevel(int index)
        {
            previewingLevel = levels[index];
            previewingLevel.Preview();
            UpdateButtons();
        }

        public void ClearData()
        {
            PlayerPrefs.DeleteKey(MAX_LEVEL_KEY);
            SceneManager.LoadScene( SceneManager.GetActiveScene().name );
        }

        public void Quit()
        {
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            Debug.LogError("Tried to quit a WebGL game");
#else
        Application.Quit();
#endif
        }
    }
}