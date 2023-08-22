using Microsoft.SharePoint.Client;

static async Task<List<ListItemDto>> FetchFolderItemsAsync(ClientContext context, SharepointSettings sharepointSettings, int quantity, int actualPage, string basePath)
{
    try
    {
        List<ListItem> listListItems = new();
        List<ListItemDto> toReturn = new();

        var toSearch = context.Web.GetFolderByServerRelativeUrl(sharepointSettings.SiteUrlBase + sharepointSettings.SubFolder + basePath);
        List library = context.Web.Lists.GetByTitle("Documents");
        context.Load(library);
        context.Load(toSearch, s => s.ServerRelativeUrl);
        context.ExecuteQuery();

        ListItemCollectionPosition position = null;

        int itemsProcessed = 0;
        bool startAddingItems = false;

        var startCounting = actualPage > 0 ? quantity * (actualPage) : 0;
        var stopCounting = actualPage > 0 ? quantity * (actualPage + 1) : quantity;

        do
        {
            context.ExecuteQuery();

            CamlQuery camlQuery = new CamlQuery
            {
                ViewXml =
                          $"<View Scope='RecursiveAll'>" +
                          $"    <RowLimit Paged='True'>{quantity}</RowLimit>" +
                          $"    <Query>" +
                          $"        <OrderBy><FieldRef Name='ID' Ascending='True' /></OrderBy>" +
                          $"        <Where>" +
                          $"            <Eq>" +
                          $"                <FieldRef Name='FSObjType' /><Value Type='Integer'>0</Value>" +
                          $"            </Eq>" +
                          $"        </Where> " +
                          $"    </Query>" +
                          $"</View>",
                ListItemCollectionPosition = position,
                FolderServerRelativeUrl = toSearch.ServerRelativeUrl,
            };

            ListItemCollection items = library.GetItems(camlQuery);
            //context.Load(items, f => f.ListItemCollectionPosition, f => f.Include(i => i.Folder, i => i.File, i => i.FileSystemObjectType));
            context.Load(items);
            context.ExecuteQuery();

            position = items.ListItemCollectionPosition;

            foreach (ListItem item in items)
            {
                itemsProcessed++;

                if (itemsProcessed > startCounting)
                {
                    startAddingItems = true;
                }

                if (startAddingItems)
                {
                    listListItems.Add(item);
                }

                if (listListItems.Count == quantity)
                {
                    break;
                }
            }

        } while (position != null && listListItems.Count < stopCounting);

        foreach (var folderOrArchive in listListItems)
        {
            var itemTemp = new ListItemDto()
            {
                Id = folderOrArchive.FileSystemObjectType == FileSystemObjectType.File ? folderOrArchive.Id : folderOrArchive.Id,
                Name = folderOrArchive.FileSystemObjectType == FileSystemObjectType.File ? folderOrArchive.DisplayName : folderOrArchive.DisplayName,
                Type = folderOrArchive.FileSystemObjectType == FileSystemObjectType.File ? EListItemType.File : EListItemType.Folder,
            };

            toReturn.Add(itemTemp);
        }

        return toReturn;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }

}
public class ListItemDto
{
    public string Name { get; set; }
    public int Id { get; set; }
    public EListItemType Type { get; set; }
}

public enum EListItemType
{
    Folder,
    File
}

public class SharepointSettings
{
    /// <summary>
    /// The base of url of the sharepoint
    /// </summary>
    public string SiteUrlBase { get; set; }

    /// <summary>
    /// The AD of the application if are using AZURE
    /// </summary>
    public string AzureId { get; set; }

    /// <summary>
    /// The thenant (AD) of your application (must be configured first)
    /// </summary>
    public string Tenant { get; set; }

    /// <summary>
    /// The physical path of the certificate
    /// </summary>
    public string CertificatePath { get; set; }

    /// <summary>
    /// The credentials configured in AZURE
    /// </summary>
    public string Pwd { get; set; }

    /// <summary>
    /// If the root is not the main path yet
    /// </summary>
    public string SubFolder { get; set; }

    /// <summary>
    /// Local where we are going to save all clients documents (root)
    /// </summary>
    public string FolderUniqueIdToSaveClientsInside { get; set; }
}