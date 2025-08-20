import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'fileType',
  standalone: true
})
export class FileTypePipe implements PipeTransform {
  transform(url: string): 'image' | 'pdf' | 'text' | 'other' {
    if (!url) return 'other';
    const ext = url.split('.').pop()?.toLowerCase() || '';
    if (['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp', 'svg'].includes(ext)) return 'image';
    if (ext === 'pdf') return 'pdf';
    if (['txt', 'log', 'md', 'csv', 'json', 'xml'].includes(ext)) return 'text';
    return 'other';
  }
}
