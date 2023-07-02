using System;
using System.Collections.Generic;
using System.Text;
using ServerCore;

namespace Server
{
    // 우선순위 큐 작동시키기
    struct JobTimerElem : IComparable<JobTimerElem>
    {
        public int execTick; // 실행시간
        public Action action; // 해야할일


        public int CompareTo(JobTimerElem other)
        {
            return other.execTick - this.execTick;
        }
    }

    class JobTimer
    {
        PriorityQueue<JobTimerElem> _pq = new PriorityQueue<JobTimerElem>();
        Object _lock = new Object();

        public static JobTimer Instance { get; } = new JobTimer();

        public void Push(Action action, int tickAfter = 0)
        {

            JobTimerElem job;
            // TickCount : 현재시간 , tickAfter : 원하는 딜레이 타임
            job.execTick = System.Environment.TickCount + tickAfter;
            job.action = action;

            lock (_lock)
            {
                _pq.Push(job);
            }
        }

        // 다음 큐(Job)을 엿보아서 기다린 시간에 따라 
        // 자동으로 실행시켜 주는 함수
        public void Flush()
        {
            while (true)
            {
                int now = Environment.TickCount;

                JobTimerElem job;

                lock (_lock)
                {
                    if(_pq.Count == 0 )
                        break;

                    job = _pq.Peek();
                    if(job.execTick > now)
                        break;

                    _pq.Pop();
                }

                job.action.Invoke();
            }
        }
    }
}
