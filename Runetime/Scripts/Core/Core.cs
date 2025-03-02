
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Mosaic
{

    /// <summary>
    /// One of the base components required for the construction of a Mosaic actor.
    /// </summary>
    [RequireComponent(typeof(CoreInput))]
    public class Core : MonoBehaviour, ICore
    {
        [Header("Default Behaviors")]
        [Tooltip("This behavior will activate when the entity is spawned.")]
        [SerializeField]
        private Behavior _spawnBehavior;
        [Tooltip("The actor will default to this behavior whenever there is not a valid behavior to transition to.")]
        [SerializeField]
        private Behavior _defaultBehavior;

        [Header("Generic Set")]
        [Tooltip("The starting behaviors of the actor.")]
        [SerializeField]
        private List<Behavior> _behaviors;
        [Tooltip("Modifiers apply persistent effects to the actor.")]
        [SerializeField]
        private List<Modifier> _modifiers;
        [SerializeField]
        private List<ModifierDecorator> _modifierDecorators;

        [Header("Starting Set Inventory")]
        [Tooltip("Sets of modules that have been grouped together.")]
        [SerializeField]
        private List<ModuleSet> _sets;


        private StateMachine _stateMachine;
        //private SetInventory _inventory;
        public CoreInput Input { get; private set; }
        public IDataTagRepository DataTags { get; private set; }
        public ModifierHandler Modifiers { get; private set; }
        public IStateMachine StateMachine => _stateMachine;
        public MonoBehaviour monoBehaviour => this;

      //  public ISetInventory Inventory => _inventory;

        private Guid _defaultSetID = Guid.Empty;

        private void Awake()
        {
            Input = GetComponent<CoreInput>();
            DataTags = new DataTagRepository();
            foreach(IInspectorDataTag idt in GetComponents<IInspectorDataTag>())
            {
                idt.AddTagToCore(this);
            }
            _stateMachine = new StateMachine(this,_spawnBehavior, _defaultBehavior, _behaviors, _defaultSetID);
            _stateMachine.Begin();
            Modifiers = new ModifierHandler(this, _modifiers, _modifierDecorators);

           // _inventory = new SetInventory(this, _sets);

        }
        public void ReSpawn()
        {
            Input.OnRespawn();// Input is reset, but handled in implementation
            DataTags.OnRespawn();//Datatags are persistent with the oprotunity to adjust on respawn
            _stateMachine.OnRespawn(_behaviors,_defaultSetID);
            Modifiers.OnRespawn(_modifiers, _modifierDecorators);//Remove all of the modifiers, replace with default
            
            
           // _inventory.OnRespawn(_sets);//Apply the default sets to the core. If
        }
        public void SetSpawn(Vector3 position, Quaternion rotation)
        {
            this.transform.position = position;
            this.transform.rotation = rotation;
        }
        public void RemoveSet(Guid setID)
        {
            _stateMachine.RemoveSet(setID);
            Modifiers.RemoveSet(setID);
        }
        public void AddModuleSet(ModuleSet set, Guid setID)
        {
            _stateMachine.AddBehavior(set.Behaviors, setID);
            Modifiers.AddModifier(set.Modifiers, this, setID);
            Modifiers.AddModifierDecorator(set.Decorators, setID);
           
            
        }
        private void OnDestroy()
        {
            Modifiers.ClearAllProcessies();

        }
    }

    public interface ICore
    {
        public MonoBehaviour monoBehaviour { get; }
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public IDataTagRepository DataTags { get; }
        public ModifierHandler Modifiers { get;}

        public void RemoveSet(Guid setID);
        public void SetSpawn(Vector3 position, Quaternion rotation);
        public void ReSpawn();
        public IStateMachine StateMachine { get; }
        //public ISetInventory Inventory { get; }


    }

}