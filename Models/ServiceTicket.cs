using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class ServiceTicket
{
    public int ServiceTicketId { get; set; }

    public int? OrderId { get; set; }

    public string CustomerRacketName { get; set; } = null!;

    public string? CurrentCondition { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public string? Status { get; set; }

    public virtual Order? Order { get; set; }
}
