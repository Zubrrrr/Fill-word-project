using UnityEngine;

public class TargetFPS : MonoBehaviour
{
    [SerializeField] private int _targetFPS = 60;

    private void Start()
    {
        SetTargetFPS();   
    }

    public void SetTargetFPS()
    {
        Application.targetFrameRate = _targetFPS;
    }
}
