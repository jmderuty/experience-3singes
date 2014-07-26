using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat
{
    public class RequestChannel<TResponse>
    {
        private Scene _scene;
        private string _route;

        private Dictionary<int, TaskCompletionSource<TResponse>> _runningRequests = new Dictionary<int, TaskCompletionSource<TResponse>>();

        public RequestChannel(Scene scene, string route)
        {
            _scene = scene;
            _route = route;

            _scene.RegisterRoute<MessageWithId<TResponse>>(_route + ".answer", OnResponse);
        }

        private Task OnResponse(RequestMessage<MessageWithId<TResponse>> response)
        {
            var request = _runningRequests[response.Content.id];
            _runningRequests.Remove(response.Content.id);

            request.SetResult(response.Content.data);
            return Task.FromResult(true);
        }
        public Task<TResponse> Send<TRequest>(IConnection connection, TRequest data)
        {
            var msg = new MessageWithId<TRequest>() { data = data, id = GenerateId() };
            var tcs = new TaskCompletionSource<TResponse>();
            _runningRequests.Add(msg.id, tcs);
            connection.Send(_route + ".request", msg);

            return tcs.Task;
           
            
        }

        int _id;
        private int GenerateId()
        {
            return Interlocked.Increment(ref _id);
        }
    }

    public class MessageWithId<T>
    {
        public T data { get; set; }

        public int id;
    }
}
