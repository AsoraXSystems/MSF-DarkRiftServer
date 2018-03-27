﻿using System.Collections.Generic;
using System.Linq;
using DarkRift;
using ServerPlugins.Game.Components;
using ServerPlugins.SharpNav.Geometry;
using Utils;
using Utils.Packets;

namespace ServerPlugins.Game.Entities
{
    public class Entity : NetworkEntity
    {
        private EntityState tmpState;
        private readonly List<Component> _components = new List<Component>();

        private Entity Target;
        public GamePlugin Game;
        public NavigationComponent agent;
        public readonly List<Player> Observers = new List<Player>();

        public Vector3 Position
        {
            get
            {
                return new Vector3(X, Y, Z);
            }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public void SetTarget(Entity target)
        {
            Target = target;
            TargetID = target == null ? 0 : target.ID; //for network sync.
        }

        public T AddComponent<T>() where T: Component, new()
        {
            var component = new T { Entity = this };
            _components.Add(component);
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            return (T)_components.FirstOrDefault(component => component is T);
        }

        public virtual void Start()
        {
            agent = GetComponent<NavigationComponent>();

            foreach (var component in _components)
            {
                component.Start();
            }
        }
        public virtual void Update()
        {
            //TODO: area of interest
            //if (Target != null && !Target.Visible)
            //    SetTarget(null);

            tmpState = State;
            foreach (var component in _components)
            {
                component.Update();
            }
        }

        public void LateUpdate()
        {
            //Send update with state has changed in Update
            if (tmpState != State)
            {
                foreach (var observer in Observers)
                {
                    observer.Client.SendMessage(Message.Create(MessageTags.ChangeState, new ChangStatePacket {EntityID = ID, State = State}),
                        SendMode.Reliable);
                }
            }
        }

        public virtual void Destroy()
        {
            foreach (var component in _components)
            {
                component.Destroy();
            }
        } 
        
        public bool IsMoving()
        {
            // -> agent.hasPath will be true if stopping distance > 0, so we can't
            //    really rely on that.
            // -> IsDirty is true while calculating the path, which is good
            // -> remainingDistance is the distance to the last path point, so it
            //    also works when clicking somewhere onto a obstacle that isn'
            //    directly reachable.
            return agent.IsDirty ||
                   agent.RemainingDistance > agent.StoppingDistance ||
                   agent.Velocity != Vector3.Zero;
        }

        public override string ToString()
        {
            return Name + "(" + ID + ")";
        }
    }
}