import sys;
import re; 
import socket;
import smtplib;
import dns.resolver;

def CheckMailSafety(email : str) -> bool: 
    regex = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    if not re.match(regex, email):
        return False
    domain = email.split('@')[1]

    try:
        mx_records = dns.resolver.resolve(domain, 'MX')
        mx_record = str(mx_records[0].exchange)
    except Exception:
        return False
    
    host = socket.gethostname()
    try:
        server = smtplib.SMTP(timeout=7)
        server.connect(mx_record, 25)
        server.helo(host)
        server.mail('ton_adresse_gmail@gmail.com') # Ton mail expéditeur
        code, _ = server.rcpt(email)
        server.quit()
        
        return code == 250
    except Exception:
        return False
    
if __name__ == "__main__":
    if len(sys.argv) > 1:
        email_to_check = sys.argv[1]
        result = CheckMailSafety(email_to_check)
        print(result)
    else:
        print("False")