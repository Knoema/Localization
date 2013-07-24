var _epli = _epli || false;
var _epls = _epls || [];

(function ($) {
	$.extend({
		localize: function (text, scope) {
				
			if (_epli) 
				_epls.push(scope.toLowerCase());		

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
