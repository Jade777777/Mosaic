using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mosaic
{
    public class StateMachine : IStateMachine
    {
        private Core _core;

        private Behavior _defaultBehavior;//only active if all else is 0. 

        private Dictionary<Guid, Behavior> _behaviorsByID = new();

        private Behavior _currentBehavior; // we save the entire module instead of something like the index because the size of the list is highly dynamic.

        private BehaviorInstance _currentInstance;


        public StateMachine(Core core, Behavior defaultModule, List<Behavior> behaviors)
        {
            this._core = core;
            this._defaultBehavior = defaultModule;
            foreach(Behavior behavior in behaviors)
            {
                AddBehavior(behavior);
            }

        }
        public void Begin()// This must be called after every aspect of the character has been initialised. 
        {
            TransformDataTag transformDataTag = _core.DataTags.GetTag<TransformDataTag>();
            transformDataTag.Position = _core.transform.position;
            transformDataTag.Rotation = _core.transform.rotation;
            

            Debug.Assert(_currentInstance == null);

            Transition(_defaultBehavior);
        }            

        public Guid AddBehavior(Behavior behavior)
        {
            Guid id = Guid.NewGuid();
            _behaviorsByID.Add(id, behavior);
            return id;
        }
        public void RemoveBehavior(Guid behaviorID)
        {
            _behaviorsByID.Remove(behaviorID);
        }

        public void Transition()
        {
            Transition((BehaviorInputType) null);
        }
        public void Transition(BehaviorInputType input)// Calculates the next apropriate behavior to transition to
        {
            
            if (_currentInstance != null)
            {
                _currentInstance.Exit();
                _currentInstance = null;
            }
            Behavior nextBehavior = Behavior.DecideNewBehavior( _behaviorsByID, _core, _currentBehavior.BehaviorTypes, input);
            EnterNewBehavior(nextBehavior);
        }


        //This would only be useful in a situation
        public bool TryTransition()
        {
            return TryTransition(null);
        }
        public bool TryTransition(BehaviorInputType input)
        {
            Behavior nextBehavior = Behavior.DecideNewBehavior(_behaviorsByID, _core, _currentBehavior.BehaviorTypes, input);
            if (nextBehavior != null)
            {
                if (_currentInstance != null)
                {
                    _currentInstance.Exit();
                    _currentInstance = null;
                }
                EnterNewBehavior(nextBehavior);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextBehavior"></param>
        public void Transition(Behavior nextBehavior)
        {
            
            if (_currentInstance != null)
            {
                _currentInstance.Exit();
                _currentInstance = null;
            }
            EnterNewBehavior(nextBehavior);
        }
        private void EnterNewBehavior(Behavior nextBehavior)//choose a new behavior, This module doesn't need to be housed within this class.
        {


            if (nextBehavior == null)
            {
                nextBehavior = _defaultBehavior;
                Debug.LogWarning("NO VALID behavior, Transitioning to default module.");
            }

            _core.Input.OverrideControl(null);
            _currentBehavior = nextBehavior;
            _currentInstance = BehaviorInstance.EnterNewInstance(nextBehavior.Instance, _core);
            Debug.Log("Transition to new behavior! " + _currentBehavior + ", " + _currentInstance);
        }

        public BehaviorInstance GetCurrentInstance()
        {
            return _currentInstance;
        }
    }
    public interface IStateMachine
    {
        public BehaviorInstance GetCurrentInstance();
        public Guid AddBehavior(Behavior behavior);
        public void RemoveBehavior(Guid behaviorID);
        public void Transition();
        public void Transition(BehaviorInputType behaviorInput);
        public bool TryTransition();
        public bool TryTransition(BehaviorInputType behaviorInput);
        public void Transition(Behavior nextBehavior);

    }
}