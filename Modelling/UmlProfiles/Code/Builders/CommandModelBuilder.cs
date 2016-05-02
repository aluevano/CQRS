﻿using System;
using System.Linq;
using Microsoft.VisualStudio.ArchitectureTools.Extensibility.Uml;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Uml.Classes;

namespace Cqrs.Modelling.UmlProfiles.Builders
{
	public abstract class CommandModelBuilder : ModelBuilder
	{
		protected CommandModelBuilder(string operationModeName, bool copyAttributes = true)
		{
			CopyAttributes = copyAttributes;
			OperationModeName = operationModeName;
		}

		protected virtual string GetPropertyInstanceName()
		{
			return string.Format("Build{0}Command", OperationModeName);
		}

		protected string OperationModeName { get; private set; }

		protected bool CopyAttributes { get; private set; }

		protected override bool ShouldCreateModel(Store store, PropertyInstance propertyInstance)
		{
			return propertyInstance.Name == GetPropertyInstanceName() && propertyInstance.Value.ToLowerInvariant() == "true";
		}

		protected override bool ShouldDeleteModel(Store store, PropertyInstance propertyInstance)
		{
			return propertyInstance.Name == GetPropertyInstanceName() && propertyInstance.Value.ToLowerInvariant() == "false";
		}

		protected override void CreateModel(Store store, PropertyInstance propertyInstance)
		{
			IClass propertyInstanceModelClass = GetPropertyInstanceModelClass(store, propertyInstance);
			// See https://msdn.microsoft.com/en-us/library/cc512845.aspx#elements for copy/paste
			using (var transaction = store.TransactionManager.BeginTransaction())
			{
				try
				{
					var modulePackage = (Package)propertyInstanceModelClass.Package;
					var commandsPackage = modulePackage.NestedPackages.SingleOrDefault(package => package.Name == "Commands") as Package;
					if (commandsPackage == null)
					{
						commandsPackage = (Package)modulePackage.CreatePackage();
						commandsPackage.Name = "Commands";
					}

					string className = string.Format("{0}{1}Command", OperationModeName, propertyInstanceModelClass.Name);
					var clonedClass = commandsPackage.OwnedElements
						// This bit filters out the associations
						.Where(element => element.AppliedStereotypes.Any())
						.Cast<Class>()
						.SingleOrDefault(element => CSharpHelper.ClassifierName(element) == className);
					if (clonedClass == null)
					{
						clonedClass = (Class)commandsPackage.CreateClass();
						clonedClass.Name = className;
					}
					AddStereotypeInstanceIfMissingRefreshOtherwise(clonedClass, propertyInstanceModelClass, "AutoGenerated");
					AddStereotypeInstanceIfMissingRefreshOtherwise(clonedClass, propertyInstanceModelClass, "Command");

					// Copy Properties
					foreach (IProperty ownedAttribute in propertyInstanceModelClass.OwnedAttributes)
					{
						AddAttributeIfMissingRefreshOtherwise(clonedClass, ownedAttribute);
					}

					// Create Association
					string operationName = string.Format("{0}{1}", OperationModeName, propertyInstanceModelClass.Name);
					AddAssociationIfMissingRefreshOtherwise(clonedClass, propertyInstanceModelClass as Class, operationName);

					// Create stub operation on propertyInstanceModelClass
					var operation = AddOperationIfMissingRefreshOtherwise(propertyInstanceModelClass, operationName);
					AddStereotypeInstanceIfMissingRefreshOtherwise(operation, operation, "AutoGenerated");
					IStereotypeInstance stereoType = AddStereotypeInstanceIfMissingRefreshOtherwise(operation, operation, "AggregateRootMethod");
					stereoType.PropertyInstances.Single(property => property.Name == "AggregateRootMethodType").Value = "Simple";
					stereoType.PropertyInstances.Single(property => property.Name == "EventName").Value = string.Format("{1}{0}d", OperationModeName, propertyInstanceModelClass.Name);

					transaction.Commit();
				}
				catch (Exception)
				{
					transaction.Rollback();
				}
			}
		}

		protected override void DeleteModel(Store store, PropertyInstance propertyInstance)
		{
			// ElementFactory elementFactory = GetMatchingElementFactory(store, propertyInstance);
			IClass propertyInstanceModelClass = GetPropertyInstanceModelClass(store, propertyInstance);
			// See https://msdn.microsoft.com/en-us/library/cc512845.aspx#elements for copy/paste
			using (var transaction = store.TransactionManager.BeginTransaction())
			{
				try
				{
					var modulePackage = (Package)propertyInstanceModelClass.Package;
					var commandsPackage = (Package)modulePackage.NestedPackages.SingleOrDefault(package => package.Name == "Commands");
					if (commandsPackage == null)
						return;

					string className = string.Format("{0}{1}Command", OperationModeName, propertyInstanceModelClass.Name);
					var clonedClass = commandsPackage.OwnedElements
						// This bit filters out the associations
						.Where(element => element.AppliedStereotypes.Any())
						.Cast<Class>()
						.SingleOrDefault(element => CSharpHelper.ClassifierName(element) == className);
					if (clonedClass == null)
						return;
					clonedClass.Delete();

					transaction.Commit();
				}
				catch (Exception)
				{
					transaction.Rollback();
				}
			}
		}
	}
}