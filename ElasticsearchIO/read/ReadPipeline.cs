using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace ElasticsearchIO
{
    public class ReadPipeline
    {

        public static ESWrapper<Person> SearchAndHandle(
            SearchParam p, 
            Func<SearchParam, ISearchResponse<Person>> search, 
            Func<ISearchResponse<Person>, ESWrapper<Person>> handle) =>
            handle(search(p));
    }
}
