from django.urls import path, include
from django.views.generic import RedirectView
from django.conf.urls.static import static
from django.conf import settings

from rest_framework.documentation import include_docs_urls


urlpatterns = [
    path('', include("hbml_service.api.urls")),
    path('docs/', include_docs_urls(title='API Documentaion')),
    path('', RedirectView.as_view(url='docs/'))

]
if settings.DEBUG:
    urlpatterns += static(settings.STATIC_URL, document_root=settings.STATIC_ROOT)
