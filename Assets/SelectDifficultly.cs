using UnityEngine;
using UnityEngine.UI;

namespace Assets
{
    [RequireComponent(typeof(Button))]
    public class SelectDifficultly : MonoBehaviour
    {
        public string Name;
        // Start is called before the first frame update
        void Start()
        {
            var b = GetComponent<Button>();
            b.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("difficultly",b.GetComponentInChildren<Text>().text.ToLower().Trim());
                PlayerPrefs.Save();
            });
        }

    }
}
