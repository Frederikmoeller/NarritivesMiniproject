using DialogueSystem;
using UnityEngine;

public class AnimationFunctions : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _marching;
    [SerializeField] private AudioClip _locking;
    [SerializeField] private AudioClip _blindfold;
    [SerializeField] private DialogueManager _dialogueManager;
    
    public void PlayMarching()
    {
        _audioSource.PlayOneShot(_marching);
    }
    
    public void PlayLocking()
    {
        _audioSource.PlayOneShot(_locking);
    }
    
    public void PlayBlindfold()
    {
        _audioSource.PlayOneShot(_blindfold);
    }

    public void StartDialogue()
    {
        _dialogueManager.StartDialogue(false);
    }
}
