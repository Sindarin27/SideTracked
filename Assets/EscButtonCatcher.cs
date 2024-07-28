using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class EscButtonCatcher : MonoBehaviour
    {
        public LevelSelector levelSelector;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (levelSelector.isActiveAndEnabled) levelSelector.Select();
                else levelSelector.gameObject.SetActive(true);
            }
        }
    }
}