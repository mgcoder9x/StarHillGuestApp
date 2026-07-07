namespace FresherDev.HMS.EntityFramework
{
    public class PaginationOutput<TModel> : IPaginationOutput<TModel>
        where TModel : class
    {
        public int Page { get; set; }

        public int Total { get; set; }

        public int PageSize { get; set; }

        public required IEnumerable<TModel> Items { get; set; }
    }
}
