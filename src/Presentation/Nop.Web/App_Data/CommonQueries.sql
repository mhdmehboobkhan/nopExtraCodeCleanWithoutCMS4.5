
UPDATE LocaleStringResource SET ResourceValue = REPLACE(ResourceValue, 'withdrawl', 'withdrawal') where id = 1
UPDATE MessageTemplate SET Body = REPLACE(Body, 'td style="background-color: #000000; padding: 10px;"', 'td style="background-color: #ffffff; padding: 10px;"') where id = 1
