using UnityEngine;

public class Letter : MonoBehaviour
{
    public char LetterChar;

    private Animator _anim;
    private bool _isDragging = false;

    private void Start()
    {
        _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        CheckMouseRelease();
    }

    private void CheckMouseRelease()
    {
        if (_isDragging && Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            _anim.SetBool("LetterSelected", false);
        }
    }

    private void OnMouseDown()
    {
        _isDragging = true;
        _anim.SetBool("LetterSelected", true);

        Debug.Log(_anim);
    }

    private void OnMouseEnter()
    {
        if (Input.GetMouseButton(0))
        {
            _isDragging = true;
            _anim.SetBool("LetterSelected", true);
        }
    }

    private void OnMouseUp()
    {
        _isDragging = false;
        _anim.SetBool("LetterSelected", false);
    }
}

