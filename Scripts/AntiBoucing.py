import sys
import re
import socket
import smtplib
import random
import string
import dns.resolver

def CheckMailSafety(email: str) -> bool:
    regex = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    if not re.match(regex, email):
        return False

    domain = email.split('@')[1]

    try:
        mx_records = dns.resolver.resolve(domain, 'MX')
        mx_record = str(sorted(mx_records, key=lambda r: r.preference)[0].exchange)
    except Exception:
        return False

    host = socket.gethostname()

    try:
        server = smtplib.SMTP(timeout=7)
        server.connect(mx_record, 25)
    except (OSError, smtplib.SMTPConnectError, socket.timeout):
        return True

    try:
        server.helo(host)
        server.mail('noreply@sendmail.local')

        random_addr = ''.join(random.choices(string.ascii_lowercase, k=20)) + '@' + domain
        catch_all_code, _ = server.rcpt(random_addr)
        if catch_all_code == 250:
            server.quit()
            return True

        code, _ = server.rcpt(email)
        server.quit()
        return code == 250

    except Exception:
        return False

if __name__ == "__main__":
    if len(sys.argv) > 1:
        print(CheckMailSafety(sys.argv[1]))
    else:
        print("False")
