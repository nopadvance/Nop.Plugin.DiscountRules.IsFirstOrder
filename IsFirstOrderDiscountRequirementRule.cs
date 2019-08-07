using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using System;
using System.Linq;

namespace Nop.Plugin.DiscountRules.IsFirstOrder
{
	public class IsFirstOrderDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
	{
		#region Fields

		private readonly IActionContextAccessor _actionContextAccessor;
		private readonly IDiscountService _discountService;
		private readonly ISettingService _settingService;
		private readonly IUrlHelperFactory _urlHelperFactory;
		private readonly IWebHelper _webHelper;
		private readonly IOrderService _orderService;

		#endregion

		#region Ctor

		public IsFirstOrderDiscountRequirementRule(IActionContextAccessor actionContextAccessor,
			IDiscountService discountService,
			ISettingService settingService,
			IUrlHelperFactory urlHelperFactory,
			IWebHelper webHelper,
			IOrderService orderService)
		{
			_actionContextAccessor = actionContextAccessor;
			_discountService = discountService;
			_settingService = settingService;
			_urlHelperFactory = urlHelperFactory;
			_webHelper = webHelper;
			_orderService = orderService;
		}

		#endregion

		#region Methods

		public DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request)
		{
			if (request == null)
                throw new ArgumentNullException(nameof(request));

            //invalid by default
            var result = new DiscountRequirementValidationResult();

            if (request.Customer == null)
                return result;

			//check if rule is applied
			var isFirstOrderRuleApplied = _settingService.GetSettingByKey<bool>(string.Format(DiscountRequirementDefaults.SettingsKey, request.DiscountRequirementId));
            if (isFirstOrderRuleApplied == false)
                return result;

			//check customer orders
			var orders = _orderService.SearchOrders(storeId: request.Store.Id, customerId: request.Customer.Id);
			result.IsValid = orders.Count == 0 ? true : false;

            return result;
		}

		public string GetConfigurationUrl(int discountId, int? discountRequirementId)
		{
			var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

			return urlHelper.Action("Configure", "DiscountRulesIsFirstOrder",
				new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.CurrentRequestProtocol);
		}

		public override void Install()
		{
			base.Install();
		}

		public override void Uninstall()
		{
			//discount requirements
			var discountRequirements = _discountService.GetAllDiscountRequirements()
				.Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountRequirementDefaults.SystemName);
			foreach (var discountRequirement in discountRequirements)
			{
				_discountService.DeleteDiscountRequirement(discountRequirement);
			}

			base.Uninstall();
		}

		#endregion
	}
}
