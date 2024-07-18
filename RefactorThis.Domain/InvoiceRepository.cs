using RefactorThis.Domain.Entities;

namespace RefactorThis.Domain {
	public class InvoiceRepository
	{
		private Invoice _invoice;

		public virtual Invoice GetInvoice(string reference) => _invoice;
		public void SaveInvoice( Invoice invoice )
		{
			//saves the invoice to the database
		}

		public void Add( Invoice invoice )
		{
			_invoice = invoice;
		}
	}
}