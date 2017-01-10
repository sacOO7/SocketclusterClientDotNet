
using System.Collections.Generic;

namespace ScClient
{
    public class Emitter
    {
        public delegate void Listener(string name,object data);
        public delegate void Ackcall(string name, object error, object data);
        public delegate void AckListener(string name,object data,Ackcall ack);


        private Dictionary <string,Listener> singlecallbacks=new Dictionary<string, Listener>();
        private Dictionary<string,AckListener> singleackcallbacks=new Dictionary<string, AckListener>();
        private Dictionary<string,Listener> publishcallbacks=new Dictionary<string, Listener>();

        public Emitter on(string Event, Listener fn)
        {
            if (singlecallbacks.ContainsKey(Event))
            {
                singlecallbacks.Remove(Event);
            }

            singlecallbacks.Add(Event,fn);

            return this;
        }

        public Emitter onSubscribe(string Event,Listener fn){

            if (publishcallbacks.ContainsKey(Event)){
                publishcallbacks.Remove(Event);
            }
            publishcallbacks.Add(Event, fn);
            return this;
        }

        public Emitter on(string Event, AckListener fn)
        {
            if (singleackcallbacks.ContainsKey(Event))
            {
                singleackcallbacks.Remove(Event);
            }
            singleackcallbacks.Add(Event,fn);
            return this;
        }

        public Emitter handleEmit(string Event, object Object)
        {
            if (singlecallbacks.ContainsKey(Event))
            {
                Listener listener = singlecallbacks[Event];
                listener(Event, Object);
            }
            return this;
        }

        public Emitter handlePublish(string Event, object Object){


            if (publishcallbacks.ContainsKey(Event))
            {
                Listener listener = publishcallbacks[Event];
                listener(Event,Object);
            }
            return this;
        }

        public bool hasEventAck(string Event)
        {
            return singleackcallbacks.ContainsKey(Event);
        }

        public Emitter handleEmitAck(string Event, object Object , Ackcall ack){

            if (singleackcallbacks.ContainsKey(Event))
            {
                AckListener listener = singleackcallbacks[Event];
                listener(Event,Object,ack);
            }
            return this;
        }


    }
}