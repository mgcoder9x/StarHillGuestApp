namespace FresherDev.HMS.EntityFramework
{
    public class PaginationInput : IPaginationInput
    {
        public PaginationInput()
        {
            this.Page = 1;
            this.PageSize = 10;
        }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public string? OrderColumn { get; set; }

        public OrderDirection Direction { get; set; }
    }
}