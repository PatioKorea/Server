using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);

    }

    // Task와 동일
    public class JobQueue : IJobQueue
    {
        Queue<Action> _jobQueue = new Queue<Action>();
        Object _lock = new Object();
        bool _flush = false;

        public void Push(Action job)
        {
            bool flush = false;

            lock (_lock)
            {
                _jobQueue.Enqueue(job);
                // 들어온 쓰레드가 일감을 처리
                // 일감 처리중 다른 쓰레드가 들어오면
                // Enqueue만 해놓고 나간다
                if (_flush == false) 
                    flush = _flush = true; // 일감처리중을 알리는 fulsh
            }

            if (flush)
                Flush();
        }

        // 일감 처리 함수
        void Flush()
        {
            while (true)
            {
                // 일감을 뽑아옴과 동시에 일감이 끝났음을 알린다
                Action action = Pop();
                if (action == null) return;
                action.Invoke(); // 일감 실행
            }
        }

        Action Pop()
        {
            lock (_lock) 
            {
                if (_jobQueue.Count == 0) 
                {
                    // 리턴하는 함수를 실행하는 쓰레드가
                    // 일을 끝났음을 알린다.
                    _flush = false;
                    return null; 
                }
                return _jobQueue.Dequeue();
            }
        }
    }
}
