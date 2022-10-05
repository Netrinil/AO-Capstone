using System;
using System.Threading.Tasks;
using GameCreator.Runtime.Common;
using UnityEngine;

namespace GameCreator.Runtime.VisualScripting
{
    [Serializable]
    public abstract class Sequence : ISequence
    {
        // EXPOSED MEMBERS: -----------------------------------------------------------------------
        
        [SerializeReference] private Track[] m_Tracks = Array.Empty<Track>();

        // MEMBERS: -------------------------------------------------------------------------------

        [NonSerialized] private float m_Time;

        [NonSerialized] private bool m_IsRunning;
        [NonSerialized] private bool m_IsCancelled;

        // PROPERTIES: ----------------------------------------------------------------------------
        
        public TimeMode.UpdateMode UpdateMode => this.TimeMode.UpdateTime;
        public float T => Mathf.Clamp01(this.Time / this.Duration);

        public float Time => this.m_Time;

        public bool IsRunning => this.m_IsRunning;
        public bool IsCancelled => AsyncManager.ExitRequest || this.m_IsCancelled;
        
        public abstract float Duration { get; }
        public abstract TimeMode TimeMode { get; }

        // EVENTS: --------------------------------------------------------------------------------

        public event Action EventStart;
        public event Action EventBeforeUpdate;
        public event Action EventAfterUpdate;
        public event Action EventComplete;
        public event Action EventCancel;
        
        // CONSTRUCTORS: --------------------------------------------------------------------------

        protected Sequence()
        {
            this.m_Time = 0;
        }

        protected Sequence(Track[] tracks) : this()
        {
            this.m_Tracks = tracks;
        }
        
        // PROTECTED METHODS: ---------------------------------------------------------------------

        protected async Task DoRun(Args args)
        {
            if (!Application.isPlaying) return;
            if (this.IsRunning) return;
            
            this.OnStart(args);
            
            while (this.IsRunning)
            {
                if (this.IsCancelled) return;
                if (this.OnRun(args)) return;
                
                await Task.Yield();
            }
        }

        protected void DoCancel(Args args)
        {
            if (!this.IsRunning) return;
            this.OnCancel(args);
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private bool OnRun(Args args)
        {
            this.OnUpdate(args);
        
            if (this.IsRunning && this.Time >= this.Duration)
            {
                this.OnComplete(args);
            }
        
            return !this.IsRunning;
        }
        
        private void OnStart(Args args)
        {
            this.m_Time = 0f;
            this.m_IsRunning = true;
            this.m_IsCancelled = false;

            foreach (ITrack track in this.m_Tracks)
            {
                track?.OnStart(this, args);
            }
            
            this.EventStart?.Invoke();
        }

        private void OnUpdate(Args args)
        {
            this.EventBeforeUpdate?.Invoke();

            float deltaTime = this.TimeMode.DeltaTime;
            this.m_Time += deltaTime;

            foreach (ITrack track in this.m_Tracks)
            {
                track?.OnUpdate(this, args);
            }
            
            this.EventAfterUpdate?.Invoke();
        }
        
        private void OnComplete(Args args)
        {
            this.m_IsRunning = false;
            foreach (ITrack track in this.m_Tracks)
            {
                track?.OnComplete(this, args);
            }
            
            this.EventComplete?.Invoke();
        }
        
        private void OnCancel(Args args)
        {
            this.m_IsCancelled = true;
            foreach (ITrack track in this.m_Tracks)
            {
                track?.OnCancel(this, args);
            }
            
            this.EventCancel?.Invoke();
        }
    }
}