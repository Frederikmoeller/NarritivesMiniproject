using UnityEngine;

public class AnimationFunctions : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _marching;
    [SerializeField] private AudioClip _locking;
    [SerializeField] private AudioClip _blindfold;
    
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
}
