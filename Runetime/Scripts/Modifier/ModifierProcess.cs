using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mosaic
{
    public class ModifierProcess
    {

        //used to track added modifiers
        private Dictionary<Guid, IModifier> _instance = new();
        
        /// <summary>
        /// Index of 0 is the Leaf Modifier. All others are decorators.
        /// </summary>
        private List<Guid> _modifiers;
        
        private Guid _id;
        private Guid _setID;
        private ICore _core;
        private ICore _origin;

        private readonly float _startTime;

        private Coroutine _process;
        private bool _started = false;
        private readonly bool _isInstance = false;

        public delegate void EndEventHandler();
        private event EndEventHandler EndEvent;




        public ModifierProcess(Modifier modifier, List<(ModifierDecorator, Guid, Guid)> decorators,ICore core, ICore origin, Guid id, Guid setID)
        {
           
            //Set Values
            _id = id;
            _setID = setID;
            _core = core;
            _origin = origin;
            _isInstance = true;
            _startTime = Time.time;

            //Instantiate modifier
            Modifier instance = ScriptableObject.Instantiate(modifier);
            instance.Initialize(this, id, setID);
            _instance.Add(id, instance);
            _modifiers = new() { id };
            
            //Instanciate decorators
            AddDecorator(decorators);
        }
        public void StartModifier()
        {
            //Start Process
            _process = _core.monoBehaviour.StartCoroutine(Process());
        }
        private IModifier CreateDecorator(ModifierDecorator decorator,Guid id, Guid setID)
        {
            ScriptableObject decoratorAsSO = (ScriptableObject)decorator;//This mess of casting feels awful and is confusing to look at, but it works so I'm leaving it for now
            IModifier instance = (IModifier)ScriptableObject.Instantiate(decoratorAsSO);
            instance.Initialize(this, id, setID);
            return instance;
        }
        public void AddDecorator(ModifierDecorator decorator, Guid id, Guid setID)
        {
            IModifier instance = CreateDecorator(decorator, id, setID);
            _instance.Add(id, instance);
            _modifiers.Add(id);
            _modifiers.Sort((x, y) => _instance[y].GetPriority().CompareTo(_instance[x].GetPriority()));
        }
        public void AddDecorator(List<(ModifierDecorator,Guid,Guid)> decorators)
        {
            foreach ((ModifierDecorator, Guid, Guid) decorator in decorators)
            {
                IModifier instance = CreateDecorator(decorator.Item1, decorator.Item2, decorator.Item3);
                _instance.Add(decorator.Item2, instance);
                _modifiers.Add(decorator.Item2);
            }
            _modifiers.Sort((x, y) => _instance[y].GetPriority().CompareTo(_instance[x].GetPriority()));
        }

        //TODO: This likely won't work, we will need to add an ID system to save 
        public void RemoveDecorator(Guid id)
        {
            _instance.Remove(id);
            _modifiers.Remove(id);
            _modifiers.Sort((x, y) => _instance[y].GetPriority().CompareTo(_instance[x].GetPriority()));
        }
        public IModifier GetChildOfDecorator(Guid id)
        {
            int indexOfComponent = _modifiers.IndexOf(id) + 1;
            
            //This will through an index out of bounds error if called by the leaf modifier; 
            return _instance[_modifiers[indexOfComponent]];
        }
        public IModifier GetModifier()
        {
            return _instance[_id];
        }
        public ICore GetCore()
        {
            return _core;
        }
        public ICore GetOrigin()
        {
            return _origin;
        }
        public float GetStartTime()
        {
            return _startTime;
        }



        //TODO: Connect functions to modifiers.
        private void Begin()
        {
            _instance[_modifiers[0]].Begin();
        }
        private bool EndCondition()
        {
            return _instance[_modifiers[0]].EndCondition();
        }
        private void Tick()
        {
            _instance[_modifiers[0]].Tick();
        }
        private YieldInstruction Yield()
        {
            return _instance[_modifiers[0]].Yield();
        }
        private void End()
        {
            _instance[_modifiers[0]].End();
        }



        public void SubscribeToEnd(EndEventHandler endMod)
        {
            EndEvent += endMod;
        }

        public void Clear()
        {
            Debug.Assert(_isInstance);
            if (_process != null)
            {
                _core.monoBehaviour.StopCoroutine(_process);
            }
            if (!_started)//If begin hasn't been called yet, we call it now to ensure consistent behavior when removing and adding on the same frame.
            {
                Begin();
            }
            End();

            foreach(KeyValuePair<Guid, IModifier> instance in _instance)
            {
                ScriptableObject.Destroy((ScriptableObject)instance.Value);    
            }

            EndEvent.Invoke();
        }

        private IEnumerator Process()
        {
            
            //We delay the process by a frame, this fixes some weird bugs. Needs more troublshooting to get to the root
            yield return null;
            _started = true;//This should be set to true just before begin so that begin doesn't get called twice.
            Begin();
            while (EndCondition())
            {
                Tick();
                yield return Yield();
            }
            //yield return null;
            Clear();
        }

    }
}

