var _epli = _epli || false;
var _epls = _epls || [];

(function ($) {
	var formatWith = function(text, formatterArguments){
		if(typeof(formatterArguments) === 'object' && formatterArguments != null){
			for (var key in formatterArguments) {
				if (formatterArguments.hasOwnProperty(key)) {
					text = text.replace("{" + key + "}", formatterArguments[key])
				}
			}
		}
		return text;
	}

	var parseMarkup = function(text){
		if(!text || text == '' || text.indexOf('[') == -1)
			return text;

		var result = text;
		var regex = /\[(.*?)\]/g;
		for(match = regex.exec(text); match; match = regex.exec(text)){

			var items = match[1].split('|')
			var innerText = items[0]

			var tag = "<a";
			if (items.length > 1)
			{
				var attrs = items.slice(1, items.length)
				for(var attr in attrs)
				{
					var attrName = attrs[attr].split('=')[0];
					tag += " " + attrName;
					if (attrs[attr].split('=').length > 1)
					{
						tag += "=\"";
						tag += attrs[attr].substring(attrName.length + 1)
						tag += "\"";
					}
				}
			}
			tag += ">" + innerText + "</a>";

			result = result.split('[' + match[1] + ']').join(tag);
		}

		return result;
	}

	var localize = function (scope, text, formatterArguments) {
				
		if (_epli) 
			_epls.push(scope.toLowerCase());		

		if ({ignoreLocalization} || scope == undefined || scope == '')
			return formatWith(text, formatterArguments);

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
				data: 'text=' + encodeURIComponent(text) + '&scope=' + encodeURIComponent(scope)
			});
		}
		else if (!t.IsDisabled) 
				translation = t.Translation;

		var result = (translation == null || translation == '') ? text : translation;
		return formatWith(result, formatterArguments);
	}

	$.extend({
		localize: localize,

		localize2: function (scope, text, formatterArguments) {
			return parseMarkup(localize(scope, text, formatterArguments));
		}
	})
})(jQuery);
