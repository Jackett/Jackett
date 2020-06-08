using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Common.Utils
{
    public static class EnumerableExtension
    {
        public static T FirstIfSingleOrDefault<T>(this IEnumerable<T> source, T replace = default)
        {
            if (source is ICollection<T> collection)
                return collection.Count == 1 ? collection.First() : replace;
            var test = source.Take(2).ToList();
            return test.Count == 1 ? test[0] : replace;
        }
    }

    public static class XElementExtension
    {
        public static XElement First(this XElement element, string name) => element.Descendants(name).First();

        public static string FirstValue(this XElement element, string name) => element.First(name).Value;
    }

    public static class TaskExtensions
    {
        public static Task<IEnumerable<TResult>> Until<TResult>(this IEnumerable<Task<TResult>> tasks, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var aggregateTask = Task.WhenAll(tasks);
            var anyTask = Task.WhenAny(timeoutTask, aggregateTask);
            var continuation = anyTask.ContinueWith((_) =>
            {
                var completedTasks = tasks.Where(t => t.Status == TaskStatus.RanToCompletion);
                var results = completedTasks.Select(t => t.Result);
                return results;
            });

            return continuation;
        }
    }
}
