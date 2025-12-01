using System.Collections;
using TMPro;
using UnityEngine;

namespace DialogueSystem.UI
{
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Typewriter Settings")]
        [SerializeField] private float charactersPerSecond = 50f;
        [SerializeField] private float startDelay = 0.1f;
        [SerializeField] private bool pauseOnPunctuation = true;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typeSound;
        [SerializeField] private float soundVolume = 0.5f;
        
        [Header("Rich Text")]
        [SerializeField] private bool preserveRichTextTags = true;
        
        private TMP_Text _textComponent;
        private Coroutine _typewriterCoroutine;
        
        // Events
        public System.Action OnCharacterTyped;
        public System.Action OnTypingCompleted;
        
        public bool IsTyping => _typewriterCoroutine != null;
        
        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
        
        public void StartTyping(string text, float speedOverride = -1f)
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);
                
            _textComponent.text = text;
            _textComponent.maxVisibleCharacters = 0;
            
            float speed = speedOverride > 0 ? speedOverride : charactersPerSecond;
            _typewriterCoroutine = StartCoroutine(TypeTextRoutine(text, speed));
        }
        
        public void Finish()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            
            _textComponent.maxVisibleCharacters = _textComponent.textInfo.characterCount;
            OnTypingCompleted?.Invoke();
        }
        
        private IEnumerator TypeTextRoutine(string text, float speed)
        {
            _textComponent.ForceMeshUpdate();
            
            yield return new WaitForSeconds(startDelay);
            
            int totalCharacters = _textComponent.textInfo.characterCount;
            float delay = 1f / speed;
            int visibleCount = 0;
            
            while (visibleCount < totalCharacters)
            {
                var charInfo = _textComponent.textInfo.characterInfo[visibleCount];
                
                // Skip invisible characters and rich text tags
                if (charInfo.isVisible)
                {
                    visibleCount++;
                    _textComponent.maxVisibleCharacters = visibleCount;
                    
                    // Play sound
                    if (typeSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(typeSound, soundVolume);
                    }
                    
                    OnCharacterTyped?.Invoke();
                    
                    // Additional pause for punctuation
                    if (pauseOnPunctuation && IsPunctuation(charInfo.character))
                    {
                        yield return new WaitForSeconds(delay * 3f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(delay);
                    }
                }
                else
                {
                    visibleCount++;
                }
            }
            
            _typewriterCoroutine = null;
            OnTypingCompleted?.Invoke();
        }
        
        private bool IsPunctuation(char character)
        {
            return character == '.' || character == '!' || character == '?' || character == ',';
        }
    }
}