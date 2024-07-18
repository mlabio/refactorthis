using RefactorThis.Domain;
using RefactorThis.Domain.Entities;
using System;
using System.Linq;

namespace RefactorThis.Persistence
{
	public class InvoiceService
	{
		private readonly InvoiceRepository _invoiceRepository;

		public InvoiceService( InvoiceRepository invoiceRepository )
		{
			_invoiceRepository = invoiceRepository;
		}

        public string ProcessPayment(Payment payment)
        {
            var inv = _invoiceRepository.GetInvoice(payment.Reference);

            if (inv == null)
            {
                throw new InvalidOperationException("There is no invoice matching this payment");
            }

            if (inv.Amount == 0)
            {
                if (inv.Payments == null || !inv.Payments.Any())
                {
                    return "no payment needed";
                }
                throw new InvalidOperationException("The invoice is in an invalid state, it has an amount of 0 and it has payments.");
            }

            if (payment.Amount > inv.Amount)
            {
                return "the payment is greater than the invoice amount";
            }

            decimal totalPayments = inv.Payments?.Sum(x => x.Amount) ?? 0;
            decimal amountRemaining = inv.Amount - inv.AmountPaid;

            if (totalPayments == inv.Amount)
            {
                return "invoice was already fully paid";
            }
            if (payment.Amount > amountRemaining)
            {
                return "the payment is greater than the partial amount remaining";
            }

            inv.AmountPaid += payment.Amount;
            if (inv.Type == InvoiceType.Commercial)
            {
                inv.TaxAmount += payment.Amount * 0.14m;
            }
            inv.Payments.Add(payment);

            if (inv.AmountPaid == inv.Amount)
            {
                return "invoice is now fully paid";
            }
            
            if(inv.AmountPaid < inv.Amount)
            {
                return "another partial payment received, still not fully paid";
            }

            return string.Empty;
        }
    }
}