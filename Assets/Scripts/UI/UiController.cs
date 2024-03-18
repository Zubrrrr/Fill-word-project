using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{
    [SerializeField] private Image _notificationImage;
    [SerializeField] private string _hexIndex;

    public void ChangeImageColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color newColor))
        {
            _notificationImage.color = newColor;
        }
        else
        {
            Debug.LogWarning("Невозможно преобразовать HEX в цвет: " + hex);
        }
    }

    private Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        else
        {
            Debug.LogError("Ошибка при конвертации HEX в цвет: " + hex);
            return Color.black;
        }
    }
}
