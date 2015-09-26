﻿using System;
using System.Runtime.Serialization;
using Cqrs.Events;
using Cqrs.Authentication;
using Cqrs.Messages;

namespace CQRSCode.ReadModel.Events
{
	public class InventoryItemDeactivated : IEvent<ISingleSignOnToken>
	{
		public InventoryItemDeactivated(Guid id)
		{
			Id = id;
		}

		public Guid Id { get; set; }

		public int Version { get; set; }

		public DateTimeOffset TimeStamp { get; set; }

		#region Implementation of IMessageWithAuthenticationToken<ISingleSignOnToken>

		public ISingleSignOnToken AuthenticationToken { get; set; }

		#endregion

		#region Implementation of IMessage

		public Guid CorrelationId { get; set; }

		[Obsolete("Use CorrelationId")]
		public Guid CorrolationId
		{
			get { return CorrelationId; }
			set { CorrelationId = value; }
		}

		[DataMember]
		public FrameworkType Framework { get; set; }

		#endregion
	}
}