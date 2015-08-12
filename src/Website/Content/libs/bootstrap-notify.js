/* 
* Project: Bootstrap Notify = v3.0.2
* Description: Turns standard Bootstrap alerts into "Growl-like" notifications.
* Author: Mouse0270 aka Robert McIntosh
* License: MIT License
* Website: https://github.com/mouse0270/bootstrap-growl
*/
(function (factory) {
	if (typeof define === 'function' && define.amd) {
		// AMD. Register as an anonymous module.
		define(['jquery'], factory);
	} else if (typeof exports === 'object') {
		// Node/CommonJS
		factory(require('jquery'));
	} else {
		// Browser globals
		factory(jQuery);
	}
}(function ($) {
	// Create the defaults once
	var defaults = {
			element: 'body',
			position: null,
			type: "info",
			allow_dismiss: true,
			newest_on_top: false,
			showProgressbar: false,
			placement: {
				from: "top",
				align: "right"
			},
			offset: 20,
			spacing: 10,
			z_index: 1031,
			delay: 5000,
			timer: 1000,
			url_target: '_blank',
			mouse_over: null,
			animate: {
				enter: 'animated fadeInDown',
				exit: 'animated fadeOutUp'
			},
			onShow: null,
			onShown: null,
			onClose: null,
			onClosed: null,
			icon_type: 'class',
			template: '<div data-notify="container" class="col-xs-11 col-sm-4 alert alert-{0}" role="alert"><button type="button" aria-hidden="true" class="close" data-notify="dismiss">&times;</button><span data-notify="icon"></span> <span data-notify="title">{1}</span> <span data-notify="message">{2}</span><div class="progress" data-notify="progressbar"><div class="progress-bar progress-bar-{0}" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;"></div></div><a href="{3}" target="{4}" data-notify="url"></a></div>'
		};

	String.format = function() {
		var str = arguments[0];
		for (var i = 1; i < arguments.length; i++) {
			str = str.replace(RegExp("\\{" + (i - 1) + "\\}", "gm"), arguments[i]);
		}
		return str;
	};

	function Notify ( element, content, options ) {
		// Setup Content of Notify
		var content = {
			content: {
				message: typeof content == 'object' ? content.message : content,
				title: content.title ? content.title : '',
				icon: content.icon ? content.icon : '',
				url: content.url ? content.url : '#',
				target: content.target ? content.target : '-'
			}
		};

		options = $.extend(true, {}, content, options);
		this.settings = $.extend(true, {}, defaults, options);
		this._defaults = defaults;
		if (this.settings.content.target == "-") {
			this.settings.content.target = this.settings.url_target;
		}
		this.animations = {
			start: 'webkitAnimationStart oanimationstart MSAnimationStart animationstart',
			end: 'webkitAnimationEnd oanimationend MSAnimationEnd animationend'
		}

		if (typeof this.settings.offset == 'number') {
		    this.settings.offset = {
		    	x: this.settings.offset,
		    	y: this.settings.offset
		    };
		}

		this.init();
	};

	$.extend(Notify.prototype, {
		init: function () {
			var self = this;

			this.buildNotify();
			if (this.settings.content.icon) {
				this.setIcon();
			}
			if (this.settings.content.url != "#") {
				this.styleURL();
			}
			this.placement();
			this.bind();

			this.notify = {
				$ele: this.$ele,
				update: function(command, update) {
					var commands = {};
					if (typeof command == "string") {					
						commands[command] = update;
					}else{
						commands = command;
					}
					for (var command in commands) {
						switch (command) {
							case "type":
								this.$ele.removeClass('alert-' + self.settings.type);
								this.$ele.find('[data-notify="progressbar"] > .progress-bar').removeClass('progress-bar-' + self.settings.type);
								self.settings.type = commands[command];
								this.$ele.addClass('alert-' + commands[command]).find('[data-notify="progressbar"] > .progress-bar').addClass('progress-bar-' + commands[command]);
								break;
							case "icon":
								var $icon = this.$ele.find('[data-notify="icon"]');
								if (self.settings.icon_type.toLowerCase() == 'class') {
									$icon.removeClass(self.settings.content.icon).addClass(commands[command]);
								}else{
									if (!$icon.is('img')) {
										$icon.find('img');
									}
									$icon.attr('src', commands[command]);
								}
								break;
							case "progress":
								var newDelay = self.settings.delay - (self.settings.delay * (commands[command] / 100));
								this.$ele.data('notify-delay', newDelay);
								this.$ele.find('[data-notify="progressbar"] > div').attr('aria-valuenow', commands[command]).css('width', commands[command] + '%');
								break;
							case "url":
								this.$ele.find('[data-notify="url"]').attr('href', commands[command]);
								break;
							case "target":
								this.$ele.find('[data-notify="url"]').attr('target', commands[command]);
								break;
							default:
								this.$ele.find('[data-notify="' + command +'"]').html(commands[command]);
						};
					}
					var posX = this.$ele.outerHeight() + parseInt(self.settings.spacing) + parseInt(self.settings.offset.y);
					self.reposition(posX);
				},
				close: function() {
					self.close();
				}
			};
		},
		buildNotify: function () {
			var content = this.settings.content;
			this.$ele = $(String.format(this.settings.template, this.settings.type, content.title, content.message, content.url, content.target));
			this.$ele.attr('data-notify-position', this.settings.placement.from + '-' + this.settings.placement.align);		
			if (!this.settings.allow_dismiss) {
				this.$ele.find('[data-notify="dismiss"]').css('display', 'none');
			}
			if ((this.settings.delay <= 0 && !this.settings.showProgressbar) || !this.settings.showProgressbar) {
				this.$ele.find('[data-notify="progressbar"]').remove();
			}
		},
		setIcon: function() {
			if (this.settings.icon_type.toLowerCase() == 'class') {
				this.$ele.find('[data-notify="icon"]').addClass(this.settings.content.icon);
			}else{
				if (this.$ele.find('[data-notify="icon"]').is('img')) {
					this.$ele.find('[data-notify="icon"]').attr('src', this.settings.content.icon);
				}else{
					this.$ele.find('[data-notify="icon"]').append('<img src="'+this.settings.content.icon+'" alt="Notify Icon" />');
				}	
			}
		},
		styleURL: function() {
			this.$ele.find('[data-notify="url"]').css({
				backgroundImage: 'url(data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7)',
				height: '100%',
				left: '0px',
				position: 'absolute',
				top: '0px',
				width: '100%',
				zIndex: this.settings.z_index + 1
			});
			this.$ele.find('[data-notify="dismiss"]').css({
				position: 'absolute',
				right: '10px',
				top: '5px',
				zIndex: this.settings.z_index + 2
			});
		},
		placement: function() {
			var self = this,
				offsetAmt = this.settings.offset.y,
				css = {
					display: 'inline-block',
					margin: '0px auto',
					position: this.settings.position ?  this.settings.position : (this.settings.element === 'body' ? 'fixed' : 'absolute'),
					transition: 'all .5s ease-in-out',
					zIndex: this.settings.z_index
				},
				hasAnimation = false,
				settings = this.settings;

			$('[data-notify-position="' + this.settings.placement.from + '-' + this.settings.placement.align + '"]:not([data-closing="true"])').each(function() {
				return offsetAmt = Math.max(offsetAmt, parseInt($(this).css(settings.placement.from)) +  parseInt($(this).outerHeight()) +  parseInt(settings.spacing));
			});
			if (this.settings.newest_on_top == true) {
				offsetAmt = this.settings.offset.y;
			}
			css[this.settings.placement.from] = offsetAmt+'px';

			switch (this.settings.placement.align) {
				case "left":
				case "right":
					css[this.settings.placement.align] = this.settings.offset.x+'px';
					break;
				case "center":
					css.left = 0;
					css.right = 0;
					break;
			}
			this.$ele.css(css).addClass(this.settings.animate.enter);

			$(this.settings.element).append(this.$ele);

			if (this.settings.newest_on_top == true) {
				offsetAmt = (parseInt(offsetAmt)+parseInt(this.settings.spacing)) + this.$ele.outerHeight();
				this.reposition(offsetAmt);
			}
			
			if ($.isFunction(self.settings.onShow)) {
				self.settings.onShow.call(this.$ele);
			}

			this.$ele.one(this.animations.start, function(event) {
				hasAnimation = true;
			}).one(this.animations.end, function(event) {
				if ($.isFunction(self.settings.onShown)) {
					self.settings.onShown.call(this);
				}
			});

			setTimeout(function() {
				if (!hasAnimation) {
					if ($.isFunction(self.settings.onShown)) {
						self.settings.onShown.call(this);
					}
				}
			}, 600);
		},
		bind: function() {
			var self = this;

			this.$ele.find('[data-notify="dismiss"]').on('click', function() {		
				self.close();
			})

			this.$ele.mouseover(function(e) {
				$(this).data('data-hover', "true");
			}).mouseout(function(e) {
				$(this).data('data-hover', "false");
			});
			this.$ele.data('data-hover', "false");

			if (this.settings.delay > 0) {
				self.$ele.data('notify-delay', self.settings.delay);
				var timer = setInterval(function() {
					var delay = parseInt(self.$ele.data('notify-delay')) - self.settings.timer;
					if ((self.$ele.data('data-hover') === 'false' && self.settings.mouse_over == "pause") || self.settings.mouse_over != "pause") {
						var percent = ((self.settings.delay - delay) / self.settings.delay) * 100;
						self.$ele.data('notify-delay', delay);
						self.$ele.find('[data-notify="progressbar"] > div').attr('aria-valuenow', percent).css('width', percent + '%');
					}
					if (delay <= -(self.settings.timer)) {
						clearInterval(timer);
						self.close();
					}
				}, self.settings.timer);
			}
		},
		close: function() {
			var self = this,
				$successors = null,
				posX = parseInt(this.$ele.css(this.settings.placement.from)),
				hasAnimation = false;

			this.$ele.data('closing', 'true').addClass(this.settings.animate.exit);
			self.reposition(posX);			
			
			if ($.isFunction(self.settings.onClose)) {
				self.settings.onClose.call(this.$ele);
			}

			this.$ele.one(this.animations.start, function(event) {
				hasAnimation = true;
			}).one(this.animations.end, function(event) {
				$(this).remove();
				if ($.isFunction(self.settings.onClosed)) {
					self.settings.onClosed.call(this);
				}
			});

			setTimeout(function() {
				if (!hasAnimation) {
					self.$ele.remove();
					if (self.settings.onClosed) {
						self.settings.onClosed(self.$ele);
					}
				}
			}, 600);
		},
		reposition: function(posX) {
			var self = this,
				notifies = '[data-notify-position="' + this.settings.placement.from + '-' + this.settings.placement.align + '"]:not([data-closing="true"])',
				$elements = this.$ele.nextAll(notifies);
			if (this.settings.newest_on_top == true) {
				$elements = this.$ele.prevAll(notifies);
			}
			$elements.each(function() {
				$(this).css(self.settings.placement.from, posX);
				posX = (parseInt(posX)+parseInt(self.settings.spacing)) + $(this).outerHeight();
			});
		}
	});

	$.notify = function ( content, options ) {
		var plugin = new Notify( this, content, options );
		return plugin.notify;
	};
	$.notifyDefaults = function( options ) {
		defaults = $.extend(true, {}, defaults, options);
		return defaults;
	};
	$.notifyClose = function( command ) {
		if (typeof command === "undefined" || command == "all") {
			$('[data-notify]').find('[data-notify="dismiss"]').trigger('click');
		}else{
			$('[data-notify-position="'+command+'"]').find('[data-notify="dismiss"]').trigger('click');
		}
	};

}));