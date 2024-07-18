using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using RefactorThis.Domain.Entities;
using RefactorThis.Persistence;

namespace RefactorThis.Domain.Tests
{
    // I'm going to comment the old tests because I'm gonna write tests based from the optimized code
    [TestFixture]
    public class InvoicePaymentProcessorTests
    {
        private readonly InvoiceService _invoiceService;
        private readonly Mock<InvoiceRepository> _invoiceRepositoryMock = new Mock<InvoiceRepository>();

        public InvoicePaymentProcessorTests()
        {
            _invoiceService = new InvoiceService(_invoiceRepositoryMock.Object);
        }

        [Test]
        public void ProcessPayment_WithNoMatchingInvoice_ThrowsInvalidOperationException()
        {
            var payment = new Payment { Reference = "NonExistentInvoice" };

            _invoiceRepositoryMock
                .Setup(repo => repo.GetInvoice(It.IsAny<string>()))
                .Returns((Invoice)null);

            var exception = Assert.Throws<InvalidOperationException>(() => _invoiceService.ProcessPayment(payment));

            Assert.AreEqual("There is no invoice matching this payment", exception.Message);
        }

        [Test]
        public void ProcessPayment_WithZeroAmountInvoice_ReturnsNoPaymentNeeded()
        {
            var payment = new Payment { Reference = "ZeroAmountInvoice" };

            _invoiceRepositoryMock
                .Setup(repo => repo.GetInvoice(It.IsAny<string>()))
                .Returns(new Invoice(_invoiceRepositoryMock.Object) { Amount = 0 });

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("no payment needed", result);
        }

        [Test]
        public void ProcessPayment_WithFullyPaidInvoice_ReturnsInvoiceAlreadyFullyPaid()
        {
            var payment = new Payment { Reference = "FullyPaidInvoice", Amount = 200 };
            var invoice = new Invoice(_invoiceRepositoryMock.Object) { 
                Amount = 200, 
                AmountPaid = 100, 
                Payments = new List<Payment>()
                {
                    new Payment()
                    {
                        Amount = 200
                    }
                }
            };

            _invoiceRepositoryMock
                .Setup(repo => repo.GetInvoice(It.IsAny<string>()))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("invoice was already fully paid", result);
        }

        [Test]
        public void ProcessPayment_WithPaymentExceedingAmountRemaining_ReturnsPaymentGreaterThanPartialAmountRemaining()
        {
            var payment = new Payment { Reference = "PartialPaidInvoice", Amount = 150 };
            var invoice = new Invoice(_invoiceRepositoryMock.Object) { 
                Amount = 200, 
                AmountPaid = 100, 
                Payments = new List<Payment>() 
            };

            _invoiceRepositoryMock
                .Setup(repo => repo.GetInvoice(It.IsAny<string>()))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("the payment is greater than the partial amount remaining", result);
        }

        [Test]
        public void ProcessPayment_WithExactPaymentAmount_ReturnsInvoiceIsNowFullyPaid()
        {
            var payment = new Payment { Reference = "PartiallyPaidInvoice", Amount = 100 };
            var invoice = new Invoice(_invoiceRepositoryMock.Object) { 
                Amount = 200, 
                AmountPaid = 100, 
                Payments = new List<Payment>(), 
                Type = InvoiceType.Standard 
            };

            _invoiceRepositoryMock
                .Setup(repo => repo.GetInvoice(It.IsAny<string>()))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("invoice is now fully paid", result);
        }

        [Test]
        public void ProcessPayment_WithPartialPayment_ReturnsAnotherPartialPaymentReceived()
        {
            var payment = new Payment { Reference = "PartiallyPaidInvoice", Amount = 50 };
            var invoice = new Invoice(_invoiceRepositoryMock.Object) { 
                Amount = 200, 
                AmountPaid = 100, 
                Payments = new List<Payment>(),
                Type = InvoiceType.Standard 
            };

            _invoiceRepositoryMock
                .Setup(repo => repo.GetInvoice(It.IsAny<string>()))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("another partial payment received, still not fully paid", result);
        }

        [Test]
        public void ProcessPayment_WhenPaymentExceedsInvoiceAmount_ReturnsPaymentGreaterThanInvoiceAmountMessage()
        {
            var payment = new Payment { Reference = "Invoice123", Amount = 2100 };

            var invoice = new Invoice(_invoiceRepositoryMock.Object)
            {
                Amount = 2000, 
                AmountPaid = 2000, 
                Payments = new List<Payment>(),
                Type = InvoiceType.Standard
            };
            _invoiceRepositoryMock.Setup(repo => repo.GetInvoice("Invoice123")).Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("the payment is greater than the invoice amount", result);
        }

        [Test]
        public void ProcessPayment_WithZeroAmountInvoiceAndPayments_ThrowsInvalidOperationException()
        {
            var payment = new Payment { Reference = "InvalidInvoice" };

            var invoice = new Invoice(_invoiceRepositoryMock.Object)
            {
                Amount = 0,
                Payments = new List<Payment> { 
                    new Payment {
                        Amount = 100 
                    } 
                }
            };

            _invoiceRepositoryMock.Setup(repo => repo.GetInvoice(payment.Reference)).Returns(invoice);

            var exception = Assert.Throws<InvalidOperationException>(() => _invoiceService.ProcessPayment(payment));

            Assert.AreEqual("The invoice is in an invalid state, it has an amount of 0 and it has payments.", exception.Message);
        }

        //[Test]
        //public void ProcessPayment_Should_ThrowException_When_NoInoiceFoundForPaymentReference( )
        //{
        //	var repo = new InvoiceRepository( );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( );
        //	var failureMessage = "";

        //	try
        //	{
        //		var result = paymentProcessor.ProcessPayment( payment );
        //	}
        //	catch ( InvalidOperationException e )
        //	{
        //		failureMessage = e.Message;
        //	}

        //	Assert.AreEqual( "There is no invoice matching this payment", failureMessage );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnFailureMessage_When_NoPaymentNeeded( )
        //{
        //	var repo = new InvoiceRepository( );

        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 0,
        //		AmountPaid = 0,
        //		Payments = null
        //	};

        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( );

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual( "no payment needed", result );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnFailureMessage_When_InvoiceAlreadyFullyPaid( )
        //{
        //	var repo = new InvoiceRepository( );

        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 10,
        //		AmountPaid = 10,
        //		Payments = new List<Payment>
        //		{
        //			new Payment
        //			{
        //				Amount = 10
        //			}
        //		}
        //	};
        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( );

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual( "invoice was already fully paid", result );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnFailureMessage_When_PartialPaymentExistsAndAmountPaidExceedsAmountDue( )
        //{
        //	var repo = new InvoiceRepository( );
        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 10,
        //		AmountPaid = 5,
        //		Payments = new List<Payment>
        //		{
        //			new Payment
        //			{
        //				Amount = 5
        //			}
        //		}
        //	};
        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( )
        //	{
        //		Amount = 6
        //	};

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual( "the payment is greater than the partial amount remaining", result );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnFailureMessage_When_NoPartialPaymentExistsAndAmountPaidExceedsInvoiceAmount( )
        //{
        //	var repo = new InvoiceRepository( );
        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 5,
        //		AmountPaid = 0,
        //		Payments = new List<Payment>( )
        //	};
        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( )
        //	{
        //		Amount = 6
        //	};

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual("the payment is greater than the partial amount remaining", result );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnFullyPaidMessage_When_PartialPaymentExistsAndAmountPaidEqualsAmountDue( )
        //{
        //	var repo = new InvoiceRepository( );
        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 10,
        //		AmountPaid = 5,
        //		Payments = new List<Payment>
        //		{
        //			new Payment
        //			{
        //				Amount = 5
        //			}
        //		}
        //	};
        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( )
        //	{
        //		Amount = 5
        //	};

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual("final partial payment received, invoice is now fully paid", result );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnFullyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidEqualsInvoiceAmount( )
        //{
        //	var repo = new InvoiceRepository( );
        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 10,
        //		AmountPaid = 0,
        //		Payments = new List<Payment>( ) { new Payment( ) { Amount = 10 } }
        //	};
        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( )
        //	{
        //		Amount = 10
        //	};

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual( "invoice was already fully paid", result );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_PartialPaymentExistsAndAmountPaidIsLessThanAmountDue( )
        //{
        //	var repo = new InvoiceRepository( );
        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 10,
        //		AmountPaid = 5,
        //		Payments = new List<Payment>
        //		{
        //			new Payment
        //			{
        //				Amount = 5
        //			}
        //		}
        //	};
        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( )
        //	{
        //		Amount = 1
        //	};

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual( "another partial payment received, still not fully paid", result );
        //}

        //[Test]
        //public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidIsLessThanInvoiceAmount( )
        //{
        //	var repo = new InvoiceRepository( );
        //	var invoice = new Invoice( repo )
        //	{
        //		Amount = 10,
        //		AmountPaid = 0,
        //		Payments = new List<Payment>( )
        //	};
        //	repo.Add( invoice );

        //	var paymentProcessor = new InvoiceService( repo );

        //	var payment = new Payment( )
        //	{
        //		Amount = 1
        //	};

        //	var result = paymentProcessor.ProcessPayment( payment );

        //	Assert.AreEqual( "invoice is now partially paid", result );
        //}
    }
}