using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.Extensions
{
    public static class Pagination
    {
        public static IQueryable<T> Paging<T>(this IQueryable<T> source, int page, int pageSize)
        {
            return source.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
