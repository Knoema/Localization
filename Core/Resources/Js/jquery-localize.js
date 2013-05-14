(function ($) {
	$.extend({
		localize: function (text, scope) {
				
			if(localizationScope){
				if($.inArray(scope.toLowerCase(), localizationScope) == -1)
					localizationScope.push(scope.toLowerCase())
			};

			if ({ignoreLocalization})
				return text;

			if(scope == undefined || scope == '')
				return text;

			var url = '{appPath}/_localization/api/push';
			var data = $.parseJSON('{data}');

			var t = null;

			$.each(data, function () {
				if (this.Text == text && this.Scope == scope) {
					t = this;
					return;
				};
			});

			var translation = null;

			if (t == null) {
				$.ajax({
					type: 'POST',
					url: url,
					data: 'text=' + text + '&scope=' + scope
				});
			}
			else
				translation = t.Translation;

			return (translation == null || translation == '') ? text : translation;
		}
	})
})(jQuery);
